using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Platform.Audit.Dtos;
using EnterpriseLogger.Application.Features.Platform.Audit.Queries;
using EnterpriseLogger.Application.Features.Platform.Dashboard.Dtos;
using EnterpriseLogger.Application.Features.Platform.Dashboard.Queries;
using EnterpriseLogger.Application.Features.Platform.Renewals.Dtos;
using EnterpriseLogger.Application.Features.Platform.Renewals.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EnterpriseLogger.Application.Common.Constants;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/platform")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Policy = AuthPolicies.PlatformAdminOnly)]
public class PlatformDashboardController : ControllerBase
{
    private readonly GetPlatformDashboardQuery _getPlatformDashboardQuery;
    private readonly GetPlatformRenewalsQuery _getPlatformRenewalsQuery;

    public PlatformDashboardController(
        GetPlatformDashboardQuery getPlatformDashboardQuery,
        GetPlatformRenewalsQuery getPlatformRenewalsQuery)
    {
        _getPlatformDashboardQuery = getPlatformDashboardQuery;
        _getPlatformRenewalsQuery = getPlatformRenewalsQuery;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<Result<PlatformDashboardDto>>> Dashboard(CancellationToken cancellationToken)
    {
        var result = await _getPlatformDashboardQuery.ExecuteAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("renewals")]
    public async Task<ActionResult<Result<PlatformRenewalListResponse>>> Renewals(CancellationToken cancellationToken)
    {
        var result = await _getPlatformRenewalsQuery.ExecuteAsync(cancellationToken);
        return Ok(result);
    }
}
