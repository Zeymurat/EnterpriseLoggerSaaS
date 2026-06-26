using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
using EnterpriseLogger.Domain.Entities;
using EnterpriseLogger.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Commands;

public class LinkSubscriptionPaymentCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<LinkSubscriptionPaymentRequest> _validator;
    private readonly IPlatformAuditService _auditService;

    public LinkSubscriptionPaymentCommand(
        IApplicationDbContext context,
        IValidator<LinkSubscriptionPaymentRequest> validator,
        IPlatformAuditService auditService)
    {
        _context = context;
        _validator = validator;
        _auditService = auditService;
    }

    public async Task<Result<LinkSubscriptionPaymentResponse>> ExecuteAsync(
        int tenantId,
        LinkSubscriptionPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<LinkSubscriptionPaymentResponse>.Failure($"Validasyon hatası: {errors}");
        }

        var payment = await _context.Payments
            .Include(p => p.Package)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId && p.TenantId == tenantId, cancellationToken);

        if (payment is null)
            return Result<LinkSubscriptionPaymentResponse>.Failure("Ödeme kaydı bulunamadı.");

        if (payment.Status == PaymentStatus.Rejected)
            return Result<LinkSubscriptionPaymentResponse>.Failure("Reddedilmiş ödeme bağlanamaz.");

        var alreadyLinked = await _context.TenantSubscriptions
            .AnyAsync(s => s.PaymentId == payment.Id, cancellationToken);

        if (alreadyLinked)
            return Result<LinkSubscriptionPaymentResponse>.Failure("Bu ödeme zaten bir aboneliğe bağlı.");

        TenantSubscription? subscription;
        if (request.SubscriptionId.HasValue)
        {
            subscription = await _context.TenantSubscriptions
                .Include(s => s.Package)
                .FirstOrDefaultAsync(
                    s => s.Id == request.SubscriptionId.Value && s.TenantId == tenantId,
                    cancellationToken);
        }
        else
        {
            var now = DateTime.UtcNow;
            subscription = await _context.TenantSubscriptions
                .Include(s => s.Package)
                .Where(s => s.TenantId == tenantId
                    && s.Status == SubscriptionStatus.PendingPayment
                    && !s.IsPaid
                    && s.EndDate > now)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (subscription is null)
            return Result<LinkSubscriptionPaymentResponse>.NotFound(
                "Ödeme bekleyen aktif abonelik bulunamadı.");

        if (subscription.IsPaid || subscription.Status == SubscriptionStatus.Active)
            return Result<LinkSubscriptionPaymentResponse>.Failure(
                "Yalnızca ödeme bekleyen aboneliklere havale bağlanabilir.");

        if (payment.PackageId != subscription.PackageId)
        {
            return Result<LinkSubscriptionPaymentResponse>.Failure(
                "Ödeme paketi ile abonelik paketi uyuşmuyor.");
        }

        var nowUtc = DateTime.UtcNow;
        subscription.PaymentId = payment.Id;
        subscription.StartDate = payment.PeriodStart;
        subscription.EndDate = payment.PeriodEnd;
        subscription.BillingCycle = payment.BillingCycle;
        subscription.IsPaid = true;
        subscription.Status = SubscriptionStatus.Active;
        subscription.GracePeriodEndDate = null;

        if (payment.Status == PaymentStatus.Pending)
        {
            payment.Status = PaymentStatus.Confirmed;
            payment.ConfirmedAt = nowUtc;
        }

        await _context.SaveChangesAsync(cancellationToken);

        subscription.Payment = payment;

        await _auditService.LogAsync(
            PlatformAuditActions.SubscriptionPaymentLinked,
            "subscription",
            subscription.Id,
            tenantId,
            $"paymentId={payment.Id}",
            cancellationToken);

        return Result<LinkSubscriptionPaymentResponse>.Success(
            new LinkSubscriptionPaymentResponse(PlatformSubscriptionMapper.ToDto(subscription)));
    }
}

public class LinkSubscriptionPaymentRequestValidator : AbstractValidator<LinkSubscriptionPaymentRequest>
{
    public LinkSubscriptionPaymentRequestValidator()
    {
        RuleFor(x => x.PaymentId).GreaterThan(0);
        RuleFor(x => x.SubscriptionId).GreaterThan(0).When(x => x.SubscriptionId.HasValue);
    }
}
