using EnterpriseLogger.Api.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Users.Commands;
using EnterpriseLogger.Application.Features.Users.Dtos;
using EnterpriseLogger.Application.Features.Users.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly GetTenantUsersQuery _getUsersQuery;
    private readonly InviteUserCommand _inviteCommand;
    private readonly UpdateUserPermissionsCommand _updatePermissionsCommand;
    private readonly UpdateUserRoleCommand _updateRoleCommand;
    private readonly DeactivateUserCommand _deactivateCommand;

    public UsersController(
        GetTenantUsersQuery getUsersQuery,
        InviteUserCommand inviteCommand,
        UpdateUserPermissionsCommand updatePermissionsCommand,
        UpdateUserRoleCommand updateRoleCommand,
        DeactivateUserCommand deactivateCommand)
    {
        _getUsersQuery = getUsersQuery;
        _inviteCommand = inviteCommand;
        _updatePermissionsCommand = updatePermissionsCommand;
        _updateRoleCommand = updateRoleCommand;
        _deactivateCommand = deactivateCommand;
    }

    [Authorize(Policy = AuthPolicies.UsersRead)]
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<UserResponseDto>>>> Get(
        CancellationToken cancellationToken)
    {
        var result = await _getUsersQuery.ExecuteAsync(cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Policy = AuthPolicies.UsersInvite)]
    [HttpPost("invite")]
    public async Task<ActionResult<Result<InviteUserResponseDto>>> Invite(
        [FromBody] InviteUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inviteCommand.ExecuteAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Policy = AuthPolicies.UsersManage)]
    [HttpPatch("{id:int}/permissions")]
    public async Task<ActionResult<Result<UserResponseDto>>> UpdatePermissions(
        int id,
        [FromBody] UpdateUserPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updatePermissionsCommand.ExecuteAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Policy = AuthPolicies.TenantRootOnly)]
    [HttpPatch("{id:int}/role")]
    public async Task<ActionResult<Result<UserResponseDto>>> UpdateRole(
        int id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateRoleCommand.ExecuteAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Policy = AuthPolicies.UsersManage)]
    [HttpPatch("{id:int}/deactivate")]
    public async Task<ActionResult<Result<UserResponseDto>>> Deactivate(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _deactivateCommand.ExecuteAsync(id, cancellationToken);
        return result.ToActionResult();
    }
}
