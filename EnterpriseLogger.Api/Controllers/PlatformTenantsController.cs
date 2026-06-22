using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Models;
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

    public PlatformTenantsController(GetPlatformTenantsQuery getPlatformTenantsQuery)
    {
        _getPlatformTenantsQuery = getPlatformTenantsQuery;
    }

    [HttpGet]
    public async Task<ActionResult<Result<PlatformTenantListResponse>>> List(
        CancellationToken cancellationToken)
    {
        var result = await _getPlatformTenantsQuery.ExecuteAsync(cancellationToken);
        return Ok(result);
    }
}
