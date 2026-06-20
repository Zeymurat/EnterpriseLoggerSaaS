using EnterpriseLogger.Application.Features.Logs.Dtos;
using EnterpriseLogger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Logs.Queries;

internal static class LogQueryFiltering
{
    public static IQueryable<SystemLog> Apply(
        IQueryable<SystemLog> query,
        GetLogsQueryParams parameters,
        bool includeDate = true)
    {
        if (parameters.LogLevels is { Count: > 0 })
        {
            var levels = parameters.LogLevels
                .Where(level => !string.IsNullOrWhiteSpace(level))
                .Select(level => level.Trim())
                .ToList();

            if (levels.Count > 0)
                query = query.Where(l => levels.Contains(l.LogLevel));
        }

        if (parameters.ApplicationNames is { Count: > 0 })
        {
            var apps = parameters.ApplicationNames
                .Where(app => !string.IsNullOrWhiteSpace(app))
                .Select(app => app.Trim())
                .ToList();

            if (apps.Count > 0)
                query = query.Where(l => apps.Contains(l.ApplicationName));
        }

        if (parameters.HttpMethods is { Count: > 0 })
        {
            var methods = parameters.HttpMethods
                .Where(method => !string.IsNullOrWhiteSpace(method))
                .Select(method => method.Trim())
                .ToList();

            if (methods.Count > 0)
                query = query.Where(l => l.HttpMethod != null && methods.Contains(l.HttpMethod));
        }

        if (parameters.StatusCodes is { Count: > 0 })
        {
            var codes = parameters.StatusCodes.ToList();
            if (codes.Count > 0)
                query = query.Where(l => l.StatusCode != null && codes.Contains(l.StatusCode.Value));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var term = parameters.Search.Trim().ToLower();
            query = query.Where(l => l.Message.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(parameters.CorrelationId))
        {
            var correlationId = parameters.CorrelationId.Trim();
            query = query.Where(l => l.CorrelationId == correlationId);
        }

        if (includeDate)
        {
            if (parameters.From.HasValue)
            {
                var from = DateTime.SpecifyKind(parameters.From.Value, DateTimeKind.Utc);
                query = query.Where(l => l.Timestamp >= from);
            }

            if (parameters.To.HasValue)
            {
                var to = DateTime.SpecifyKind(parameters.To.Value, DateTimeKind.Utc);
                query = query.Where(l => l.Timestamp <= to);
            }
        }

        return query;
    }

    public static async Task<LogLevelSummaryDto> BuildSummaryAsync(
        IQueryable<SystemLog> query,
        CancellationToken cancellationToken)
    {
        var summaryRow = await query
            .GroupBy(_ => 1)
            .Select(g => new LogLevelSummaryDto(
                g.Count(),
                g.Count(l => l.LogLevel == "Info"),
                g.Count(l => l.LogLevel == "Warning"),
                g.Count(l => l.LogLevel == "Error")))
            .FirstOrDefaultAsync(cancellationToken);

        return summaryRow ?? new LogLevelSummaryDto(0, 0, 0, 0);
    }
}
