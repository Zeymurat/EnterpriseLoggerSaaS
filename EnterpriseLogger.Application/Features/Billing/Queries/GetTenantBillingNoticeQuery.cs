using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Billing.Dtos;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Billing.Queries;

public class GetTenantBillingNoticeQuery
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantProvider _tenantProvider;

    public GetTenantBillingNoticeQuery(
        IApplicationDbContext context,
        ICurrentTenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<TenantBillingNoticeDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (_tenantProvider.TenantId is not int tenantId)
            return Result<TenantBillingNoticeDto>.Forbidden("Tenant bağlamı gerekli.");

        var now = DateTime.UtcNow;

        var pendingRenewal = await _context.TenantSubscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Where(s => s.TenantId == tenantId
                && s.Status == SubscriptionStatus.PendingPayment
                && !s.IsPaid
                && s.EndDate > now)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (pendingRenewal is null)
        {
            return Result<TenantBillingNoticeDto>.Success(
                new TenantBillingNoticeDto(false, null, null, null, null, null));
        }

        return Result<TenantBillingNoticeDto>.Success(
            new TenantBillingNoticeDto(
                true,
                pendingRenewal.Package.Name,
                pendingRenewal.GracePeriodEndDate,
                pendingRenewal.EndDate,
                "Paketiniz yenilendi. Havale/EFT ödemenizi tamamlayın; aksi halde süre sonunda Free pakete düşersiniz.",
                pendingRenewal.BillingCycle));
    }
}
