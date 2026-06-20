using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Logs.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Logs.Queries;

public class GetLogFilterOptionsQuery
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentTenantProvider _tenantProvider;

    public GetLogFilterOptionsQuery(IApplicationDbContext context, ICurrentTenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<LogFilterOptionsDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.IsResolved)
        {
            return Result<LogFilterOptionsDto>.Failure(
                "Tenant kimliği çözümlenemedi. X-Api-Key header gerekli.");
        }

        var options = await LogFilterOptionsLoader.LoadAsync(_context, cancellationToken);
        return Result<LogFilterOptionsDto>.Success(options);
    }
}
