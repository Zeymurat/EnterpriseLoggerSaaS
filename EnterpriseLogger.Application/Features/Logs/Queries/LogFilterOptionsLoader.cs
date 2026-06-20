using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Features.Logs.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Logs.Queries;

internal static class LogFilterOptionsLoader
{
    public static async Task<LogFilterOptionsDto> LoadAsync(
        IApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var applicationNames = await context.SystemLogs
            .AsNoTracking()
            .Select(l => l.ApplicationName)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        var httpMethods = await context.SystemLogs
            .AsNoTracking()
            .Where(l => l.HttpMethod != null)
            .Select(l => l.HttpMethod!)
            .Distinct()
            .OrderBy(method => method)
            .ToListAsync(cancellationToken);

        var statusCodes = await context.SystemLogs
            .AsNoTracking()
            .Where(l => l.StatusCode != null)
            .Select(l => l.StatusCode!.Value)
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);

        return new LogFilterOptionsDto(applicationNames, httpMethods, statusCodes);
    }
}
