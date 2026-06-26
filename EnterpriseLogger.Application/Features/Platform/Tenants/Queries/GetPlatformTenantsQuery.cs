using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Queries;

public class GetPlatformTenantsQuery
{
    private readonly IApplicationDbContext _context;

    public GetPlatformTenantsQuery(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PlatformTenantListResponse>> ExecuteAsync(
        PlatformTenantListFilter? filter,
        int page = 1,
        int pageSize = PlatformTenantQueryBuilder.DefaultPageSize,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        filter ??= new PlatformTenantListFilter(null, null, null, null, null, null, null);
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1
            ? PlatformTenantQueryBuilder.DefaultPageSize
            : Math.Min(pageSize, PlatformTenantQueryBuilder.MaxPageSize);

        var now = DateTime.UtcNow;
        var query = PlatformTenantQueryBuilder.ApplyFilters(_context.Tenants.AsNoTracking(), filter);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await PlatformTenantQueryBuilder
            .ApplySort(query, sortBy, sortDescending)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new PlatformTenantListItemDto(
                t.Id,
                t.Name,
                t.IsActive,
                t.CreatedAt,
                t.Users.Count,
                _context.SystemLogs.IgnoreQueryFilters().Count(l => l.TenantId == t.Id),
                t.Users.Where(u => u.Role == TenantUserRole.Root).Select(u => u.Email).FirstOrDefault(),
                t.Users.Where(u => u.Role == TenantUserRole.Root).Select(u => u.Phone).FirstOrDefault(),
                t.Subscriptions
                    .Where(s => SubscriptionHelper.ActiveStatuses.Contains(s.Status) && s.EndDate > now)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => s.Package.Name)
                    .FirstOrDefault(),
                t.Subscriptions
                    .Where(s => SubscriptionHelper.ActiveStatuses.Contains(s.Status) && s.EndDate > now)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => (SubscriptionStatus?)s.Status)
                    .FirstOrDefault(),
                t.Subscriptions
                    .Where(s => SubscriptionHelper.ActiveStatuses.Contains(s.Status) && s.EndDate > now)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => (DateTime?)s.StartDate)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Result<PlatformTenantListResponse>.Success(
            new PlatformTenantListResponse(items, totalCount, page, pageSize));
    }
}
