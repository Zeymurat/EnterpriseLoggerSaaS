using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Commands;

public class DeletePlatformTenantCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IPlatformAuditService _auditService;

    public DeletePlatformTenantCommand(
        IApplicationDbContext context,
        IPlatformAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result<bool>> ExecuteAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
            return Result<bool>.NotFound("Tenant bulunamadı.");

        var (canDelete, blockedReason) = await TenantDeletePolicy.EvaluateAsync(
            _context,
            tenantId,
            cancellationToken);

        if (!canDelete)
            return Result<bool>.Failure(blockedReason ?? "Bu müşteri silinemez.");

        var subscriptions = await _context.TenantSubscriptions
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        _context.TenantSubscriptions.RemoveRange(subscriptions);

        var payments = await _context.Payments
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        _context.Payments.RemoveRange(payments);

        var userIds = await _context.Users
            .Where(u => u.TenantId == tenantId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (userIds.Count > 0)
        {
            var userPermissions = await _context.UserPermissions
                .Where(up => userIds.Contains(up.UserId))
                .ToListAsync(cancellationToken);
            _context.UserPermissions.RemoveRange(userPermissions);
        }

        var users = await _context.Users
            .Where(u => u.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        _context.Users.RemoveRange(users);

        var logs = await _context.SystemLogs
            .IgnoreQueryFilters()
            .Where(log => log.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        _context.SystemLogs.RemoveRange(logs);

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            PlatformAuditActions.TenantDeleted,
            "tenant",
            tenantId,
            tenantId,
            $"name={tenant.Name}",
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
