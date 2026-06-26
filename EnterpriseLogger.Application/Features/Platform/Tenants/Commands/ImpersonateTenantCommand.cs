using EnterpriseLogger.Application.Common.Authorization;
using EnterpriseLogger.Application.Common.Interfaces;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Auth.Dtos;
using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
using EnterpriseLogger.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseLogger.Application.Features.Platform.Tenants.Commands;

public class ImpersonateTenantCommand
{
    private static readonly TimeSpan TicketTtl = TimeSpan.FromMinutes(2);

    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IImpersonationTicketStore _ticketStore;

    public ImpersonateTenantCommand(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IImpersonationTicketStore ticketStore)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _ticketStore = ticketStore;
    }

    public async Task<Result<ImpersonateTenantTicketDto>> ExecuteAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
            return Result<ImpersonateTenantTicketDto>.NotFound("Tenant bulunamadı.");

        if (!tenant.IsActive)
            return Result<ImpersonateTenantTicketDto>.Forbidden("Tenant pasif durumda. Login-as kullanılamaz.");

        var rootUser = await _context.Users
            .AsNoTracking()
            .Include(u => u.Tenant)
            .Include(u => u.UserPermissions)
                .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(
                u => u.TenantId == tenantId
                    && u.Role == TenantUserRole.Root
                    && u.IsActive,
                cancellationToken);

        if (rootUser is null)
            return Result<ImpersonateTenantTicketDto>.NotFound("Tenant için aktif Root kullanıcı bulunamadı.");

        var permissions = PermissionResolver.Resolve(
            rootUser.Role,
            rootUser.UserPermissions.Select(up => up.Permission.Code));

        var token = _jwtTokenService.GenerateToken(
            rootUser.Id,
            rootUser.Email,
            rootUser.TenantId,
            rootUser.Role.ToString(),
            permissions,
            DateTime.UtcNow);

        var session = new LoginResponse(
            token.AccessToken,
            token.ExpiresInSeconds,
            new UserInfoDto(
                rootUser.Id,
                rootUser.Email,
                rootUser.Phone,
                rootUser.Role.ToString(),
                rootUser.TenantId,
                rootUser.Tenant.Name,
                permissions));

        var ticket = _ticketStore.CreateTicket(session, TicketTtl);

        return Result<ImpersonateTenantTicketDto>.Success(
            new ImpersonateTenantTicketDto(ticket, (int)TicketTtl.TotalSeconds));
    }
}
