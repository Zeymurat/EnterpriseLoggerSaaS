using System.Globalization;
using System.Text;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Logs.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Logs.Queries;

public class ExportLogsQuery
{
    public const int MaxExportRows = 10_000;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantProvider _tenantProvider;

    public ExportLogsQuery(IApplicationDbContext context, ICurrentTenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<LogExportResultDto>> ExecuteAsync(
        GetLogsQueryParams parameters,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.IsResolved)
        {
            return Result<LogExportResultDto>.Failure(
                "Tenant kimliği çözümlenemedi. X-Api-Key header gerekli.");
        }

        var filteredQuery = LogQueryFiltering.Apply(
            _context.SystemLogs.AsNoTracking(),
            parameters,
            includeDate: true);

        var totalMatching = await filteredQuery.CountAsync(cancellationToken);
        var truncated = totalMatching > MaxExportRows;

        var logs = await filteredQuery
            .OrderByDescending(l => l.Timestamp)
            .Take(MaxExportRows)
            .Select(l => new LogResponseDto(
                l.Id,
                l.TenantId,
                l.ApplicationName,
                l.LogLevel,
                l.Message,
                l.Timestamp,
                l.HttpMethod,
                l.RequestPath,
                l.StatusCode,
                l.CorrelationId,
                l.ActorIdentifier,
                l.ExceptionType))
            .ToListAsync(cancellationToken);

        var csv = BuildCsv(logs);
        var fileName = $"logs-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        return Result<LogExportResultDto>.Success(
            new LogExportResultDto(
                Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(),
                fileName,
                logs.Count,
                totalMatching,
                truncated));
    }

    private static string BuildCsv(IReadOnlyList<LogResponseDto> logs)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            "Id,Timestamp,ApplicationName,LogLevel,Message,HttpMethod,RequestPath,StatusCode,CorrelationId,ActorIdentifier,ExceptionType");

        foreach (var log in logs)
        {
            builder.Append(log.Id)
                .Append(',')
                .Append(EscapeCsv(log.Timestamp.ToString("o", CultureInfo.InvariantCulture)))
                .Append(',')
                .Append(EscapeCsv(log.ApplicationName))
                .Append(',')
                .Append(EscapeCsv(log.LogLevel))
                .Append(',')
                .Append(EscapeCsv(log.Message))
                .Append(',')
                .Append(EscapeCsv(log.HttpMethod))
                .Append(',')
                .Append(EscapeCsv(log.RequestPath))
                .Append(',')
                .Append(log.StatusCode?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)
                .Append(',')
                .Append(EscapeCsv(log.CorrelationId))
                .Append(',')
                .Append(EscapeCsv(log.ActorIdentifier))
                .Append(',')
                .AppendLine(EscapeCsv(log.ExceptionType));
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!needsQuotes)
            return value;

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
