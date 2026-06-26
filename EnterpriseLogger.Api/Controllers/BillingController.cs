using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Billing.Dtos;
using EnterpriseLogger.Application.Features.Billing.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class BillingController : ControllerBase
{
    private readonly GetTenantBillingNoticeQuery _getTenantBillingNoticeQuery;
    private readonly GetTenantUsageQuery _getTenantUsageQuery;

    public BillingController(
        GetTenantBillingNoticeQuery getTenantBillingNoticeQuery,
        GetTenantUsageQuery getTenantUsageQuery)
    {
        _getTenantBillingNoticeQuery = getTenantBillingNoticeQuery;
        _getTenantUsageQuery = getTenantUsageQuery;
    }

    [HttpGet("notice")]
    public async Task<ActionResult<Result<TenantBillingNoticeDto>>> Notice(CancellationToken cancellationToken)
    {
        var result = await _getTenantBillingNoticeQuery.ExecuteAsync(cancellationToken);

        if (!result.IsSuccess && result.ErrorKind == ResultErrorKind.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, result);

        return Ok(result);
    }

    [HttpGet("usage")]
    public async Task<ActionResult<Result<TenantUsageDto>>> Usage(CancellationToken cancellationToken)
    {
        var result = await _getTenantUsageQuery.ExecuteAsync(cancellationToken);

        if (!result.IsSuccess && result.ErrorKind == ResultErrorKind.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, result);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}
