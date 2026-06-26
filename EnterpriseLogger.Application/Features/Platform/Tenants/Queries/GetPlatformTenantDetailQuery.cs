using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Subscriptions;
using EnterpriseLogger.Application.Features.Platform.Tenants;
using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Queries;

public class GetPlatformTenantDetailQuery
{
    private readonly IApplicationDbContext _context;

    public GetPlatformTenantDetailQuery(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PlatformTenantDetailDto>> ExecuteAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.IsActive,
                t.CreatedAt,
                UserCount = t.Users.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
            return Result<PlatformTenantDetailDto>.NotFound("Tenant bulunamadı.");

        var logCount = await _context.SystemLogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(log => log.TenantId == tenantId, cancellationToken);

        var rootUser = await _context.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Role == TenantUserRole.Root)
            .OrderByDescending(u => u.IsActive)
            .ThenBy(u => u.Id)
            .Select(u => new PlatformTenantRootUserDto(u.Id, u.Email, u.Phone))
            .FirstOrDefaultAsync(cancellationToken);

        var history = await _context.TenantSubscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Include(s => s.Payment)
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync(cancellationToken);

        var current = await SubscriptionHelper.GetActiveSubscriptionAsync(
            _context.TenantSubscriptions,
            tenantId,
            cancellationToken);

        var historyDtos = history.Select(PlatformSubscriptionMapper.ToDto).ToList();
        var currentDto = current is null ? null : PlatformSubscriptionMapper.ToDto(current);

        var (canDelete, deleteBlockedReason) = await TenantDeletePolicy.EvaluateAsync(
            _context,
            tenantId,
            cancellationToken);

        var detail = new PlatformTenantDetailDto(
            tenant.Id,
            tenant.Name,
            tenant.IsActive,
            tenant.CreatedAt,
            tenant.UserCount,
            logCount,
            rootUser,
            currentDto,
            historyDtos,
            canDelete,
            deleteBlockedReason);

        return Result<PlatformTenantDetailDto>.Success(detail);
    }
}
