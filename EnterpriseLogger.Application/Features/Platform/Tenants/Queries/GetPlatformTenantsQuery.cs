using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
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
        CancellationToken cancellationToken = default)
    {
        var tenants = await _context.Tenants
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.IsActive,
                t.CreatedAt,
                UserCount = t.Users.Count
            })
            .ToListAsync(cancellationToken);

        var logCounts = await _context.SystemLogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .GroupBy(log => log.TenantId)
            .Select(group => new { TenantId = group.Key, Count = group.LongCount() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        var items = tenants
            .Select(t => new PlatformTenantListItemDto(
                t.Id,
                t.Name,
                t.IsActive,
                t.CreatedAt,
                t.UserCount,
                logCounts.GetValueOrDefault(t.Id)))
            .ToList();

        return Result<PlatformTenantListResponse>.Success(new PlatformTenantListResponse(items));
    }
}
