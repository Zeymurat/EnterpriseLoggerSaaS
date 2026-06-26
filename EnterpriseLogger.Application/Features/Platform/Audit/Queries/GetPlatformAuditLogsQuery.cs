using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Platform.Audit.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Audit.Queries;

public class GetPlatformAuditLogsQuery
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    private readonly IApplicationDbContext _context;

    public GetPlatformAuditLogsQuery(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PlatformAuditLogListResponse>> ExecuteAsync(
        int page = 1,
        int pageSize = DefaultPageSize,
        int? tenantId = null,
        string? action = null,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var query = _context.PlatformAuditLogs.AsNoTracking();

        if (tenantId is int tid)
            query = query.Where(log => log.TenantId == tid);

        if (!string.IsNullOrWhiteSpace(action))
        {
            var actionFilter = action.Trim();
            query = query.Where(log => log.Action.Contains(actionFilter));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(log => log.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new PlatformAuditLogListItemDto(
                log.Id,
                log.PlatformAdminId,
                log.ActorEmail,
                log.Action,
                log.EntityType,
                log.EntityId,
                log.TenantId,
                log.Tenant != null ? log.Tenant.Name : null,
                log.Details,
                log.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PlatformAuditLogListResponse>.Success(
            new PlatformAuditLogListResponse(items, totalCount, page, pageSize));
    }
}
