using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
using EnterpriseLogger.Domain.Entities;
using EnterpriseLogger.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Commands;

public class AssignTenantSubscriptionCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<AssignTenantSubscriptionRequest> _validator;

    public AssignTenantSubscriptionCommand(
        IApplicationDbContext context,
        IValidator<AssignTenantSubscriptionRequest> validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<Result<AssignTenantSubscriptionResponse>> ExecuteAsync(
        int tenantId,
        AssignTenantSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<AssignTenantSubscriptionResponse>.Failure($"Validasyon hatası: {errors}");
        }

        var tenantExists = await _context.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId, cancellationToken);

        if (!tenantExists)
            return Result<AssignTenantSubscriptionResponse>.NotFound("Tenant bulunamadı.");

        var package = await _context.Packages
            .FirstOrDefaultAsync(p => p.Id == request.PackageId && p.IsAvailable, cancellationToken);

        if (package is null)
            return Result<AssignTenantSubscriptionResponse>.Failure("Seçilen paket bulunamadı veya satışa kapalı.");

        Payment? linkedPayment = null;
        if (request.PaymentId.HasValue)
        {
            linkedPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == request.PaymentId.Value && p.TenantId == tenantId, cancellationToken);

            if (linkedPayment is null)
                return Result<AssignTenantSubscriptionResponse>.Failure("Ödeme kaydı bulunamadı veya bu tenant'a ait değil.");

            if (linkedPayment.Status == PaymentStatus.Rejected)
                return Result<AssignTenantSubscriptionResponse>.Failure("Reddedilmiş ödeme aboneliğe bağlanamaz.");

            var alreadyLinked = await _context.TenantSubscriptions
                .AnyAsync(s => s.PaymentId == linkedPayment.Id, cancellationToken);

            if (alreadyLinked)
                return Result<AssignTenantSubscriptionResponse>.Failure("Bu ödeme zaten bir aboneliğe bağlı.");
        }

        var now = DateTime.UtcNow;
        var isPaid = request.IsPaid;
        if (linkedPayment is { Status: PaymentStatus.Confirmed })
            isPaid = true;

        var activeSubscriptions = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId
                && (s.Status == SubscriptionStatus.Active
                    || s.Status == SubscriptionStatus.PendingPayment
                    || s.Status == SubscriptionStatus.PastDue)
                && s.EndDate > now)
            .ToListAsync(cancellationToken);

        foreach (var active in activeSubscriptions)
            SubscriptionHelper.Supersede(active, now);

        var subscription = SubscriptionHelper.CreateSubscription(
            tenantId,
            package,
            request.BillingCycle,
            isPaid,
            request.AutoRenew,
            request.GracePeriodEndDate,
            now);

        if (linkedPayment is not null)
        {
            subscription.PaymentId = linkedPayment.Id;
            subscription.StartDate = linkedPayment.PeriodStart;
            subscription.EndDate = linkedPayment.PeriodEnd;
            subscription.BillingCycle = linkedPayment.BillingCycle;

            if (linkedPayment.Status == PaymentStatus.Pending && isPaid)
            {
                linkedPayment.Status = PaymentStatus.Confirmed;
                linkedPayment.ConfirmedAt = now;
            }

            if (linkedPayment.Status == PaymentStatus.Confirmed)
            {
                subscription.IsPaid = true;
                subscription.Status = SubscriptionStatus.Active;
                subscription.GracePeriodEndDate = null;
            }
        }
        else if (!isPaid && request.GracePeriodEndDate.HasValue)
        {
            subscription.GracePeriodEndDate =
                DateTime.SpecifyKind(request.GracePeriodEndDate.Value, DateTimeKind.Utc);
        }

        _context.TenantSubscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        subscription.Package = package;

        return Result<AssignTenantSubscriptionResponse>.Success(
            new AssignTenantSubscriptionResponse(PlatformSubscriptionMapper.ToDto(subscription)));
    }
}

public class AssignTenantSubscriptionRequestValidator : AbstractValidator<AssignTenantSubscriptionRequest>
{
    public AssignTenantSubscriptionRequestValidator()
    {
        RuleFor(x => x.PackageId).GreaterThan(0);
        RuleFor(x => x.BillingCycle).IsInEnum();
        RuleFor(x => x.PaymentId).GreaterThan(0).When(x => x.PaymentId.HasValue);
    }
}
