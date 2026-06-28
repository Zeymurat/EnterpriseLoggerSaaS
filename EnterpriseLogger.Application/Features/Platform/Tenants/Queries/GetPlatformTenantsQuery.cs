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

        var tenantRows = await PlatformTenantQueryBuilder
            .ApplySort(query, sortBy, sortDescending)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.IsActive,
                t.CreatedAt,
                UserCount = t.Users.Count
            })
            .ToListAsync(cancellationToken);

        var tenantIds = tenantRows.Select(t => t.Id).ToList();
        var enrichment = await PlatformTenantEnrichmentLoader.LoadAsync(
            _context,
            tenantIds,
            now,
            cancellationToken);

        var items = tenantRows.Select(t =>
        {
            var details = enrichment[t.Id];
            return new PlatformTenantListItemDto(
                t.Id,
                t.Name,
                t.IsActive,
                t.CreatedAt,
                t.UserCount,
                details.LogCount,
                details.RootEmail,
                details.RootPhone,
                details.PackageName,
                details.SubscriptionStatus,
                details.SubscriptionStart);
        }).ToList();

        return Result<PlatformTenantListResponse>.Success(
            new PlatformTenantListResponse(items, totalCount, page, pageSize));
    }
}
