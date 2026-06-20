using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Logs.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Logs.Queries;

/// <summary>
/// Tenant'a ait logları sayfalı listeler.
/// Manuel TenantId filtresi yok — EF Core Global Query Filter devreye girer.
/// </summary>
public class GetLogsQuery
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantProvider _tenantProvider;

    public GetLogsQuery(IApplicationDbContext context, ICurrentTenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<LogListResponseDto>> ExecuteAsync(
        GetLogsQueryParams parameters,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.IsResolved)
        {
            return Result<LogListResponseDto>.Failure(
                "Tenant kimliği çözümlenemedi. X-Api-Key header gerekli.");
        }

        var page = parameters.Page < 1 ? 1 : parameters.Page;
        var pageSize = parameters.PageSize < 1
            ? DefaultPageSize
            : Math.Min(parameters.PageSize, MaxPageSize);

        var isDateFiltered = parameters.From.HasValue || parameters.To.HasValue;
        var tenantQuery = _context.SystemLogs.AsNoTracking();
        var filteredQuery = LogQueryFiltering.Apply(tenantQuery, parameters, includeDate: true);

        var filteredSummary = await LogQueryFiltering.BuildSummaryAsync(filteredQuery, cancellationToken);
        LogLevelSummaryDto? overallSummary = null;

        if (isDateFiltered)
            overallSummary = await LogQueryFiltering.BuildSummaryAsync(tenantQuery, cancellationToken);

        var items = await filteredQuery
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
                l.ExceptionType,
                null))
            .ToListAsync(cancellationToken);

        var correlationCounts = await LogQueryFiltering.CountCorrelationChainsAsync(
            tenantQuery,
            items.Select(item => item.CorrelationId),
            cancellationToken);

        var itemsWithTraceCounts = items
            .Select(item => LogQueryFiltering.WithCorrelationLogCount(item, correlationCounts))
            .ToList();

        var availableFilters = await LogFilterOptionsLoader.LoadAsync(_context, cancellationToken);

        return Result<LogListResponseDto>.Success(new LogListResponseDto(
            itemsWithTraceCounts,
            filteredSummary.Total,
            page,
            pageSize,
            filteredSummary,
            availableFilters,
            overallSummary,
            isDateFiltered));
    }
}
