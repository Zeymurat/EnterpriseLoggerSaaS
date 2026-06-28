using System.Globalization;
using System.Text;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
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

        var tenantRows = await PlatformTenantQueryBuilder
            .ApplySort(query, sortBy, sortDescending)
            .Take(PlatformTenantQueryBuilder.MaxExportRows)
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

        var builder = new StringBuilder();
        builder.AppendLine("Id,Firma,Durum,Root E-posta,Root Telefon,Paket,Abonelik Durumu,Paket Baslangic,Kayit,Kullanici,Log");

        foreach (var row in tenantRows)
        {
            var details = enrichment[row.Id];
            builder.Append(row.Id).Append(',');
            builder.Append(Csv(row.Name)).Append(',');
            builder.Append(row.IsActive ? "Aktif" : "Pasif").Append(',');
            builder.Append(Csv(details.RootEmail)).Append(',');
            builder.Append(Csv(details.RootPhone)).Append(',');
            builder.Append(Csv(details.PackageName)).Append(',');
            builder.Append(Csv(SubscriptionStatusLabels.ToTurkish(details.SubscriptionStatus))).Append(',');
            builder.Append(Csv(details.SubscriptionStart?.ToString("o", CultureInfo.InvariantCulture))).Append(',');
            builder.Append(Csv(row.CreatedAt.ToString("o", CultureInfo.InvariantCulture))).Append(',');
            builder.Append(row.UserCount).Append(',');
            builder.Append(details.LogCount);
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
