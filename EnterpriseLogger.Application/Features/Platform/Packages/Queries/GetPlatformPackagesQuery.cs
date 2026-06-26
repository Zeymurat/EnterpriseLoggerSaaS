using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Platform.Packages.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Packages.Queries;

public class GetPlatformPackagesQuery
{
    private readonly IApplicationDbContext _context;

    public GetPlatformPackagesQuery(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PlatformPackageListResponse>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var packages = await _context.Packages
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);

        var activeTenantCounts = await _context.TenantSubscriptions
            .AsNoTracking()
            .Where(s => SubscriptionHelper.ActiveStatuses.Contains(s.Status) && s.EndDate > now)
            .GroupBy(s => s.PackageId)
            .Select(g => new
            {
                PackageId = g.Key,
                Count = g.Select(s => s.TenantId).Distinct().Count(),
            })
            .ToDictionaryAsync(x => x.PackageId, x => x.Count, cancellationToken);

        var items = packages
            .Select(package => PlatformPackageMapper.ToDto(
                package,
                activeTenantCounts.GetValueOrDefault(package.Id)))
            .ToList();

        return Result<PlatformPackageListResponse>.Success(new PlatformPackageListResponse(items));
    }
}
