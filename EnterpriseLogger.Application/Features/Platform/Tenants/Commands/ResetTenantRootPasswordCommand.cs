using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Common.Security;
using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Commands;

public class ResetTenantRootPasswordCommand
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPlatformAuditService _auditService;

    public ResetTenantRootPasswordCommand(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IPlatformAuditService auditService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }

    public async Task<Result<ResetTenantRootPasswordResponse>> ExecuteAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var rootUser = await _context.Users
            .FirstOrDefaultAsync(
                u => u.TenantId == tenantId && u.Role == TenantUserRole.Root && u.IsActive,
                cancellationToken);

        if (rootUser is null)
            return Result<ResetTenantRootPasswordResponse>.NotFound("Aktif Root kullanıcı bulunamadı.");

        var temporaryPassword = TemporaryPasswordGenerator.Generate();
        rootUser.PasswordHash = _passwordHasher.Hash(temporaryPassword);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            PlatformAuditActions.TenantRootPasswordReset,
            "user",
            rootUser.Id,
            tenantId,
            cancellationToken: cancellationToken);

        return Result<ResetTenantRootPasswordResponse>.Success(
            new ResetTenantRootPasswordResponse(temporaryPassword));
    }
}
