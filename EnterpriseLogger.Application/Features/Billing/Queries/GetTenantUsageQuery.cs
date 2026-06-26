using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Packages;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Billing.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Billing.Queries;

public class GetTenantUsageQuery
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantProvider _tenantProvider;

    public GetTenantUsageQuery(
        IApplicationDbContext context,
        ICurrentTenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<TenantUsageDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_tenantProvider.TenantId is not int tenantId)
            return Result<TenantUsageDto>.Forbidden("Tenant bağlamı gerekli.");

        var subscription = await SubscriptionHelper.GetActiveSubscriptionAsync(
            _context.TenantSubscriptions.AsNoTracking(),
            tenantId,
            cancellationToken);

        if (subscription is null)
            return Result<TenantUsageDto>.Failure("Aktif abonelik bulunamadı.");

        var monthlyLogCount = await PackageQuotaHelper.GetMonthlyLogCountAsync(
            _context,
            tenantId,
            cancellationToken);

        var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
        var logsLastMinute = await _context.SystemLogs
            .AsNoTracking()
            .CountAsync(log => log.TenantId == tenantId && log.Timestamp >= oneMinuteAgo, cancellationToken);

        return Result<TenantUsageDto>.Success(
            new TenantUsageDto(
                subscription.Package.Name,
                monthlyLogCount,
                subscription.Package.MonthlyRequestLimit,
                logsLastMinute,
                subscription.Package.MaxLogsPerMinute));
    }
}
