using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Logs.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Logs.Queries;

/// <summary>
/// Tenant'a ait logları listeler.
/// Manuel TenantId filtresi yok — EF Core Global Query Filter devreye girer.
/// </summary>
public class GetLogsQuery
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantProvider _tenantProvider;

    public GetLogsQuery(IApplicationDbContext context, ICurrentTenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<IReadOnlyList<LogResponseDto>>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.IsResolved)
        {
            return Result<IReadOnlyList<LogResponseDto>>.Failure(
                "Tenant kimliği çözümlenemedi. X-Api-Key header gerekli.");
        }

        var logs = await _context.SystemLogs
            .AsNoTracking()
            .OrderByDescending(l => l.Timestamp)
            .Select(l => new LogResponseDto(
                l.Id,
                l.TenantId,
                l.ApplicationName,
                l.LogLevel,
                l.Message,
                l.Timestamp))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<LogResponseDto>>.Success(logs);
    }
}
