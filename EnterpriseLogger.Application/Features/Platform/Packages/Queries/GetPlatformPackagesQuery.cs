using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Platform.Packages.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Packages.Queries;

public class GetPlatformPackagesQuery
{
    private readonly IApplicationDbContext _context;

    public GetPlatformPackagesQuery(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PlatformPackageListResponse>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var packages = await _context.Packages
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);

        var items = packages.Select(PlatformPackageMapper.ToDto).ToList();
        return Result<PlatformPackageListResponse>.Success(new PlatformPackageListResponse(items));
    }
}
