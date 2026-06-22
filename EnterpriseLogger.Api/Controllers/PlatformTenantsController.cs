using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Auth.Dtos;
using EnterpriseLogger.Application.Features.Platform.Tenants.Commands;
using EnterpriseLogger.Application.Features.Platform.Tenants.Dtos;
using EnterpriseLogger.Application.Features.Platform.Tenants.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/platform/tenants")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Policy = AuthPolicies.PlatformAdminOnly)]
public class PlatformTenantsController : ControllerBase
{
    private readonly GetPlatformTenantsQuery _getPlatformTenantsQuery;
    private readonly GetPlatformTenantDetailQuery _getPlatformTenantDetailQuery;
    private readonly AssignTenantSubscriptionCommand _assignTenantSubscriptionCommand;
    private readonly ImpersonateTenantCommand _impersonateTenantCommand;

    public PlatformTenantsController(
        GetPlatformTenantsQuery getPlatformTenantsQuery,
        GetPlatformTenantDetailQuery getPlatformTenantDetailQuery,
        AssignTenantSubscriptionCommand assignTenantSubscriptionCommand,
        ImpersonateTenantCommand impersonateTenantCommand)
    {
        _getPlatformTenantsQuery = getPlatformTenantsQuery;
        _getPlatformTenantDetailQuery = getPlatformTenantDetailQuery;
        _assignTenantSubscriptionCommand = assignTenantSubscriptionCommand;
        _impersonateTenantCommand = impersonateTenantCommand;
    }

    [HttpGet]
    public async Task<ActionResult<Result<PlatformTenantListResponse>>> List(
        CancellationToken cancellationToken)
    {
        var result = await _getPlatformTenantsQuery.ExecuteAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result<PlatformTenantDetailDto>>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _getPlatformTenantDetailQuery.ExecuteAsync(id, cancellationToken);

        if (!result.IsSuccess && result.ErrorKind == ResultErrorKind.NotFound)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost("{id:int}/subscription")]
    public async Task<ActionResult<Result<AssignTenantSubscriptionResponse>>> AssignSubscription(
        int id,
        [FromBody] AssignTenantSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assignTenantSubscriptionCommand.ExecuteAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorKind == ResultErrorKind.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/impersonate")]
    public async Task<ActionResult<Result<LoginResponse>>> Impersonate(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _impersonateTenantCommand.ExecuteAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorKind == ResultErrorKind.NotFound)
                return NotFound(result);

            if (result.ErrorKind == ResultErrorKind.Forbidden)
                return StatusCode(StatusCodes.Status403Forbidden, result);

            return BadRequest(result);
        }

        return Ok(result);
    }
}
