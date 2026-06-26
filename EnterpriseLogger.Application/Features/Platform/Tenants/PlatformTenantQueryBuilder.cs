using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
using EnterpriseLogger.Domain.Entities;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants;

internal static class PlatformTenantQueryBuilder
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
    public const int MaxExportRows = 5_000;

    public static IQueryable<Tenant> ApplyFilters(IQueryable<Tenant> query, PlatformTenantListFilter filter)
    {
        var now = DateTime.UtcNow;

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

        return query;
    }

    public static IQueryable<Tenant> ApplySort(IQueryable<Tenant> query, string? sortBy, bool descending)
    {
        return (sortBy?.Trim().ToLowerInvariant()) switch
        {
            "name" => descending
                ? query.OrderByDescending(t => t.Name)
                : query.OrderBy(t => t.Name),
            "isactive" => descending
                ? query.OrderByDescending(t => t.IsActive).ThenByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.IsActive).ThenByDescending(t => t.CreatedAt),
            "usercount" => descending
                ? query.OrderByDescending(t => t.Users.Count).ThenByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.Users.Count).ThenByDescending(t => t.CreatedAt),
            _ => descending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),
        };
    }
}
