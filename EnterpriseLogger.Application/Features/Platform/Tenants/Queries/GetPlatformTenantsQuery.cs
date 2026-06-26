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
        CancellationToken cancellationToken = default)
    {
        filter ??= new PlatformTenantListFilter(null, null, null, null, null, null, null);
        var now = DateTime.UtcNow;

        var query = _context.Tenants.AsNoTracking();

        if (filter.IsActive is bool isActive)
            query = query.Where(t => t.IsActive == isActive);

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            var name = filter.Name.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(filter.RootEmail))
        {
            var email = filter.RootEmail.Trim().ToLower();
            query = query.Where(t => t.Users.Any(u =>
                u.Role == TenantUserRole.Root && u.Email.ToLower().Contains(email)));
        }

        if (!string.IsNullOrWhiteSpace(filter.RootPhone))
        {
            var phone = filter.RootPhone.Trim();
            query = query.Where(t => t.Users.Any(u =>
                u.Role == TenantUserRole.Root && u.Phone.Contains(phone)));
        }

        if (!string.IsNullOrWhiteSpace(filter.PackageCode))
        {
            var packageCode = filter.PackageCode.Trim();
            query = query.Where(t => t.Subscriptions.Any(s =>
                SubscriptionHelper.ActiveStatuses.Contains(s.Status)
                && s.EndDate > now
                && s.Package.Code == packageCode));
        }

        if (filter.SubscriptionStartFrom is DateTime startFrom)
        {
            var fromUtc = DateTime.SpecifyKind(startFrom.Date, DateTimeKind.Utc);
            query = query.Where(t => t.Subscriptions.Any(s =>
                SubscriptionHelper.ActiveStatuses.Contains(s.Status)
                && s.EndDate > now
                && s.StartDate >= fromUtc));
        }

        if (filter.SubscriptionStartTo is DateTime startTo)
        {
            var toUtc = DateTime.SpecifyKind(startTo.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(t => t.Subscriptions.Any(s =>
                SubscriptionHelper.ActiveStatuses.Contains(s.Status)
                && s.EndDate > now
                && s.StartDate < toUtc));
        }

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
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

        return Result<PlatformTenantListResponse>.Success(new PlatformTenantListResponse(items));
    }
}
