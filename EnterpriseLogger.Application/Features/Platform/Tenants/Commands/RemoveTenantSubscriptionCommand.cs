using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Commands;

public class RemoveTenantSubscriptionCommand
{
    private readonly IApplicationDbContext _context;
    private readonly SubscriptionLifecycleService _lifecycleService;
    private readonly IPlatformAuditService _auditService;

    public RemoveTenantSubscriptionCommand(
        IApplicationDbContext context,
        SubscriptionLifecycleService lifecycleService,
        IPlatformAuditService auditService)
    {
        _context = context;
        _lifecycleService = lifecycleService;
        _auditService = auditService;
    }

    public async Task<Result<RemoveTenantSubscriptionResponse>> ExecuteAsync(
        int tenantId,
        int subscriptionId,
        CancellationToken cancellationToken = default)
    {
        var subscription = await _context.TenantSubscriptions
            .Include(s => s.Package)
            .FirstOrDefaultAsync(
                s => s.Id == subscriptionId && s.TenantId == tenantId,
                cancellationToken);

        if (subscription is null)
            return Result<RemoveTenantSubscriptionResponse>.NotFound("Abonelik kaydı bulunamadı.");

        var now = DateTime.UtcNow;
        var wasActiveAssignment = ActiveStatuses.Contains(subscription.Status)
            && subscription.EndDate > now;

        _context.TenantSubscriptions.Remove(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        if (wasActiveAssignment)
            await _lifecycleService.EnsureFreeSubscriptionAsync(tenantId, now, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var current = await SubscriptionHelper.GetActiveSubscriptionAsync(
            _context.TenantSubscriptions,
            tenantId,
            cancellationToken);

        await _auditService.LogAsync(
            PlatformAuditActions.SubscriptionRemoved,
            "subscription",
            subscriptionId,
            tenantId,
            cancellationToken: cancellationToken);

        return Result<RemoveTenantSubscriptionResponse>.Success(
            new RemoveTenantSubscriptionResponse(
                current is null ? null : PlatformSubscriptionMapper.ToDto(current)));
    }

    private static readonly SubscriptionStatus[] ActiveStatuses =
    [
        SubscriptionStatus.Active,
        SubscriptionStatus.PendingPayment,
        SubscriptionStatus.PastDue
    ];
}
