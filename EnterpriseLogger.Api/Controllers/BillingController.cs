using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Billing.Dtos;
using EnterpriseLogger.Application.Features.Billing.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/billing")]
[Authorize(Policy = AuthPolicies.BillingAccess)]
public class BillingController : ControllerBase
{
    private readonly GetTenantBillingNoticeQuery _getTenantBillingNoticeQuery;
    private readonly GetTenantUsageQuery _getTenantUsageQuery;
    private readonly GetTenantBillingOverviewQuery _getTenantBillingOverviewQuery;

    public BillingController(
        GetTenantBillingNoticeQuery getTenantBillingNoticeQuery,
        GetTenantUsageQuery getTenantUsageQuery,
        GetTenantBillingOverviewQuery getTenantBillingOverviewQuery)
    {
        _getTenantBillingNoticeQuery = getTenantBillingNoticeQuery;
        _getTenantUsageQuery = getTenantUsageQuery;
        _getTenantBillingOverviewQuery = getTenantBillingOverviewQuery;
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

    [HttpGet("overview")]
    public async Task<ActionResult<Result<TenantBillingOverviewDto>>> Overview(CancellationToken cancellationToken)
    {
        var result = await _getTenantBillingOverviewQuery.ExecuteAsync(cancellationToken);

        if (!result.IsSuccess && result.ErrorKind == ResultErrorKind.Forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, result);

        return Ok(result);
    }
}
