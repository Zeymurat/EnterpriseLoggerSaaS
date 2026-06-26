using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Platform.Payments.Dtos;
using EnterpriseLogger.Domain.Entities;
using EnterpriseLogger.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Payments.Commands;

public class RecordPlatformPaymentCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<RecordPlatformPaymentRequest> _validator;
    private readonly IPlatformAuditService _auditService;

    public RecordPlatformPaymentCommand(
        IApplicationDbContext context,
        IValidator<RecordPlatformPaymentRequest> validator,
        IPlatformAuditService auditService)
    {
        _context = context;
        _validator = validator;
        _auditService = auditService;
    }

    public async Task<Result<PlatformPaymentDto>> ExecuteAsync(
        RecordPlatformPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<PlatformPaymentDto>.Failure($"Validasyon hatası: {errors}");
        }

        var tenant = await _context.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);
        if (tenant is null)
            return Result<PlatformPaymentDto>.NotFound("Tenant bulunamadı.");

        var package = await _context.Packages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PackageId, cancellationToken);
        if (package is null)
            return Result<PlatformPaymentDto>.NotFound("Paket bulunamadı.");

        var periodStart = DateTime.SpecifyKind(request.PeriodStart, DateTimeKind.Utc);
        var periodEnd = SubscriptionHelper.CalculateEndDate(request.BillingCycle, periodStart);

        var payment = new Payment
        {
            TenantId = request.TenantId,
            PackageId = request.PackageId,
            Amount = request.Amount,
            BillingCycle = request.BillingCycle,
            ReferenceNumber = request.ReferenceNumber.Trim(),
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Notes = request.Notes?.Trim() ?? string.Empty,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            PlatformAuditActions.PaymentRecorded,
            "payment",
            payment.Id,
            request.TenantId,
            $"amount={payment.Amount}",
            cancellationToken);

        return Result<PlatformPaymentDto>.Success(Map(payment, tenant.Name, package.Name, null));
    }

    internal static PlatformPaymentDto Map(
        Payment payment,
        string tenantName,
        string packageName,
        int? linkedSubscriptionId) =>
        new(
            payment.Id,
            payment.TenantId,
            tenantName,
            payment.PackageId,
            packageName,
            payment.Amount,
            payment.Currency,
            payment.Method,
            payment.ReferenceNumber,
            payment.Status,
            payment.BillingCycle,
            payment.PeriodStart,
            payment.PeriodEnd,
            payment.Notes,
            payment.CreatedAt,
            payment.ConfirmedAt,
            linkedSubscriptionId);
}

public class RecordPlatformPaymentRequestValidator : AbstractValidator<RecordPlatformPaymentRequest>
{
    public RecordPlatformPaymentRequestValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.PackageId).GreaterThan(0);
        RuleFor(x => x.BillingCycle).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ReferenceNumber).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes != null);
    }
}

public class ConfirmPlatformPaymentCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IPlatformAuditService _auditService;

    public ConfirmPlatformPaymentCommand(
        IApplicationDbContext context,
        IPlatformAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result<PlatformPaymentDto>> ExecuteAsync(
        int paymentId,
        ConfirmPlatformPaymentRequest request,
        int? platformAdminId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Tenant)
            .Include(p => p.Package)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment is null)
            return Result<PlatformPaymentDto>.NotFound("Ödeme kaydı bulunamadı.");

        if (payment.Status != PaymentStatus.Pending)
            return Result<PlatformPaymentDto>.Failure("Yalnızca bekleyen ödemeler onaylanabilir.");

        payment.Status = PaymentStatus.Confirmed;
        payment.ConfirmedAt = DateTime.UtcNow;
        payment.ConfirmedByPlatformAdminId = platformAdminId;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            payment.Notes = request.Notes.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        var linkedId = await _context.TenantSubscriptions
            .Where(s => s.PaymentId == payment.Id)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        await _auditService.LogAsync(
            PlatformAuditActions.PaymentConfirmed,
            "payment",
            payment.Id,
            payment.TenantId,
            cancellationToken: cancellationToken);

        return Result<PlatformPaymentDto>.Success(
            RecordPlatformPaymentCommand.Map(payment, payment.Tenant.Name, payment.Package.Name, linkedId));
    }
}

public class RejectPlatformPaymentCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IPlatformAuditService _auditService;

    public RejectPlatformPaymentCommand(
        IApplicationDbContext context,
        IPlatformAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result<PlatformPaymentDto>> ExecuteAsync(
        int paymentId,
        RejectPlatformPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Tenant)
            .Include(p => p.Package)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment is null)
            return Result<PlatformPaymentDto>.NotFound("Ödeme kaydı bulunamadı.");

        if (payment.Status != PaymentStatus.Pending)
            return Result<PlatformPaymentDto>.Failure("Yalnızca bekleyen ödemeler reddedilebilir.");

        payment.Status = PaymentStatus.Rejected;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            payment.Notes = request.Notes.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            PlatformAuditActions.PaymentRejected,
            "payment",
            payment.Id,
            payment.TenantId,
            cancellationToken: cancellationToken);

        return Result<PlatformPaymentDto>.Success(
            RecordPlatformPaymentCommand.Map(payment, payment.Tenant.Name, payment.Package.Name, null));
    }
}

public class UpdatePlatformPaymentCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<UpdatePlatformPaymentRequest> _validator;
    private readonly IPlatformAuditService _auditService;

    public UpdatePlatformPaymentCommand(
        IApplicationDbContext context,
        IValidator<UpdatePlatformPaymentRequest> validator,
        IPlatformAuditService auditService)
    {
        _context = context;
        _validator = validator;
        _auditService = auditService;
    }

    public async Task<Result<PlatformPaymentDto>> ExecuteAsync(
        int paymentId,
        UpdatePlatformPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<PlatformPaymentDto>.Failure($"Validasyon hatası: {errors}");
        }

        var payment = await _context.Payments
            .Include(p => p.Tenant)
            .Include(p => p.Package)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment is null)
            return Result<PlatformPaymentDto>.NotFound("Ödeme kaydı bulunamadı.");

        var linked = await _context.TenantSubscriptions
            .AnyAsync(s => s.PaymentId == payment.Id, cancellationToken);

        if (linked)
            return Result<PlatformPaymentDto>.Failure("Aboneliğe bağlı ödeme düzenlenemez.");

        if (payment.Status != PaymentStatus.Pending)
            return Result<PlatformPaymentDto>.Failure("Yalnızca bekleyen ödemeler düzenlenebilir.");

        var package = await _context.Packages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PackageId, cancellationToken);

        if (package is null)
            return Result<PlatformPaymentDto>.NotFound("Paket bulunamadı.");

        var periodStart = DateTime.SpecifyKind(request.PeriodStart, DateTimeKind.Utc);
        payment.PackageId = request.PackageId;
        payment.BillingCycle = request.BillingCycle;
        payment.Amount = request.Amount;
        payment.ReferenceNumber = request.ReferenceNumber.Trim();
        payment.PeriodStart = periodStart;
        payment.PeriodEnd = SubscriptionHelper.CalculateEndDate(request.BillingCycle, periodStart);
        payment.Notes = request.Notes?.Trim() ?? string.Empty;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            PlatformAuditActions.PaymentUpdated,
            "payment",
            payment.Id,
            payment.TenantId,
            cancellationToken: cancellationToken);

        return Result<PlatformPaymentDto>.Success(
            RecordPlatformPaymentCommand.Map(payment, payment.Tenant.Name, package.Name, null));
    }
}

public class UpdatePlatformPaymentRequestValidator : AbstractValidator<UpdatePlatformPaymentRequest>
{
    public UpdatePlatformPaymentRequestValidator()
    {
        RuleFor(x => x.PackageId).GreaterThan(0);
        RuleFor(x => x.BillingCycle).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.ReferenceNumber).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes != null);
    }
}

public class DeletePlatformPaymentCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IPlatformAuditService _auditService;

    public DeletePlatformPaymentCommand(
        IApplicationDbContext context,
        IPlatformAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result<bool>> ExecuteAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment is null)
            return Result<bool>.NotFound("Ödeme kaydı bulunamadı.");

        var linked = await _context.TenantSubscriptions
            .AnyAsync(s => s.PaymentId == payment.Id, cancellationToken);

        if (linked)
            return Result<bool>.Failure("Aboneliğe bağlı ödeme silinemez.");

        if (payment.Status == PaymentStatus.Confirmed)
            return Result<bool>.Failure("Onaylanmış ödeme silinemez. Yalnızca bekleyen veya reddedilen kayıtlar silinebilir.");

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            PlatformAuditActions.PaymentDeleted,
            "payment",
            paymentId,
            payment.TenantId,
            cancellationToken: cancellationToken);

        return Result<bool>.Success(true);
    }
}
