using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Commands;

public class SetTenantStatusCommand
{
    private readonly IApplicationDbContext _context;

    public SetTenantStatusCommand(IApplicationDbContext context)
    {
        _context = context;
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

        return Result<bool>.Success(true);
    }
}
