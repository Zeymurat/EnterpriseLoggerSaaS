using System.Globalization;
using System.Text;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Queries;

public class ExportPlatformTenantsQuery
{
    private readonly IApplicationDbContext _context;

    public ExportPlatformTenantsQuery(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<byte[]>> ExecuteAsync(
        PlatformTenantListFilter? filter,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        filter ??= new PlatformTenantListFilter(null, null, null, null, null, null, null);
        var now = DateTime.UtcNow;

        var query = PlatformTenantQueryBuilder.ApplyFilters(_context.Tenants.AsNoTracking(), filter);

        var rows = await PlatformTenantQueryBuilder
            .ApplySort(query, sortBy, sortDescending)
            .Take(PlatformTenantQueryBuilder.MaxExportRows)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.IsActive,
                t.CreatedAt,
                UserCount = t.Users.Count,
                LogCount = _context.SystemLogs.IgnoreQueryFilters().Count(l => l.TenantId == t.Id),
                RootEmail = t.Users.Where(u => u.Role == TenantUserRole.Root).Select(u => u.Email).FirstOrDefault(),
                RootPhone = t.Users.Where(u => u.Role == TenantUserRole.Root).Select(u => u.Phone).FirstOrDefault(),
                PackageName = t.Subscriptions
                    .Where(s => SubscriptionHelper.ActiveStatuses.Contains(s.Status) && s.EndDate > now)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => s.Package.Name)
                    .FirstOrDefault(),
                SubscriptionStatus = t.Subscriptions
                    .Where(s => SubscriptionHelper.ActiveStatuses.Contains(s.Status) && s.EndDate > now)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => (SubscriptionStatus?)s.Status)
                    .FirstOrDefault(),
                SubscriptionStart = t.Subscriptions
                    .Where(s => SubscriptionHelper.ActiveStatuses.Contains(s.Status) && s.EndDate > now)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => (DateTime?)s.StartDate)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("Id,Firma,Durum,Root E-posta,Root Telefon,Paket,Abonelik Durumu,Paket Baslangic,Kayit,Kullanici,Log");

        foreach (var row in rows)
        {
            builder.Append(row.Id).Append(',');
            builder.Append(Csv(row.Name)).Append(',');
            builder.Append(row.IsActive ? "Aktif" : "Pasif").Append(',');
            builder.Append(Csv(row.RootEmail)).Append(',');
            builder.Append(Csv(row.RootPhone)).Append(',');
            builder.Append(Csv(row.PackageName)).Append(',');
            builder.Append(Csv(row.SubscriptionStatus?.ToString())).Append(',');
            builder.Append(Csv(row.SubscriptionStart?.ToString("o", CultureInfo.InvariantCulture))).Append(',');
            builder.Append(Csv(row.CreatedAt.ToString("o", CultureInfo.InvariantCulture))).Append(',');
            builder.Append(row.UserCount).Append(',');
            builder.Append(row.LogCount);
            builder.AppendLine();
        }

        return Result<byte[]>.Success(Encoding.UTF8.GetBytes(builder.ToString()));
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
