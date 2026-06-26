using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
using EnterpriseLogger.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Commands;

public class CancelTenantSubscriptionCommand
{
    private readonly IApplicationDbContext _context;
    private readonly SubscriptionLifecycleService _lifecycleService;
    private readonly IValidator<CancelTenantSubscriptionRequest> _validator;
    private readonly IPlatformAuditService _auditService;

    public CancelTenantSubscriptionCommand(
        IApplicationDbContext context,
        SubscriptionLifecycleService lifecycleService,
        IValidator<CancelTenantSubscriptionRequest> validator,
        IPlatformAuditService auditService)
    {
        _context = context;
        _lifecycleService = lifecycleService;
        _validator = validator;
        _auditService = auditService;
    }

    public async Task<Result<CancelTenantSubscriptionResponse>> ExecuteAsync(
        int tenantId,
        CancelTenantSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<CancelTenantSubscriptionResponse>.Failure($"Validasyon hatası: {errors}");
        }

        var tenantExists = await _context.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == tenantId, cancellationToken);

        if (!tenantExists)
            return Result<CancelTenantSubscriptionResponse>.NotFound("Tenant bulunamadı.");

        var now = DateTime.UtcNow;
        await _lifecycleService.CancelActiveSubscriptionsAsync(
            tenantId,
            request.Reason,
            now,
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await _lifecycleService.EnsureFreeSubscriptionAsync(tenantId, now, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var current = await SubscriptionHelper.GetActiveSubscriptionAsync(
            _context.TenantSubscriptions,
            tenantId,
            cancellationToken);

        if (current is null)
        {
            return Result<CancelTenantSubscriptionResponse>.Failure(
                "Abonelik iptal edildi ancak Free paket atanamadı.");
        }

        await _auditService.LogAsync(
            PlatformAuditActions.SubscriptionCancelled,
            "subscription",
            current.Id,
            tenantId,
            request.Reason,
            cancellationToken);

        return Result<CancelTenantSubscriptionResponse>.Success(
            new CancelTenantSubscriptionResponse(PlatformSubscriptionMapper.ToDto(current)));
    }
}

public class CancelTenantSubscriptionRequestValidator : AbstractValidator<CancelTenantSubscriptionRequest>
{
    public CancelTenantSubscriptionRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
