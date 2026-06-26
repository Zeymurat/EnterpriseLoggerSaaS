using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Commands;

public class SetTenantStatusCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IPlatformAuditService _auditService;

    public SetTenantStatusCommand(
        IApplicationDbContext context,
        IPlatformAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Result<bool>> ExecuteAsync(
        int tenantId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
            return Result<bool>.NotFound("Tenant bulunamadı.");

        tenant.IsActive = isActive;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            PlatformAuditActions.TenantStatusChanged,
            "tenant",
            tenantId,
            tenantId,
            $"isActive={isActive}",
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
