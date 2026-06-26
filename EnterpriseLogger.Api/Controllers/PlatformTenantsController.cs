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
    private readonly DeletePlatformTenantCommand _deletePlatformTenantCommand;
    private readonly LinkSubscriptionPaymentCommand _linkSubscriptionPaymentCommand;
    private readonly CancelTenantSubscriptionCommand _cancelTenantSubscriptionCommand;
    private readonly RemoveTenantSubscriptionCommand _removeTenantSubscriptionCommand;
    private readonly SetTenantStatusCommand _setTenantStatusCommand;
    private readonly ResetTenantRootPasswordCommand _resetTenantRootPasswordCommand;

    public PlatformTenantsController(
        GetPlatformTenantsQuery getPlatformTenantsQuery,
        GetPlatformTenantDetailQuery getPlatformTenantDetailQuery,
        AssignTenantSubscriptionCommand assignTenantSubscriptionCommand,
        ImpersonateTenantCommand impersonateTenantCommand,
        DeletePlatformTenantCommand deletePlatformTenantCommand,
        LinkSubscriptionPaymentCommand linkSubscriptionPaymentCommand,
        CancelTenantSubscriptionCommand cancelTenantSubscriptionCommand,
        RemoveTenantSubscriptionCommand removeTenantSubscriptionCommand,
        SetTenantStatusCommand setTenantStatusCommand,
        ResetTenantRootPasswordCommand resetTenantRootPasswordCommand)
    {
        _getPlatformTenantsQuery = getPlatformTenantsQuery;
        _getPlatformTenantDetailQuery = getPlatformTenantDetailQuery;
        _assignTenantSubscriptionCommand = assignTenantSubscriptionCommand;
        _impersonateTenantCommand = impersonateTenantCommand;
        _deletePlatformTenantCommand = deletePlatformTenantCommand;
        _linkSubscriptionPaymentCommand = linkSubscriptionPaymentCommand;
        _cancelTenantSubscriptionCommand = cancelTenantSubscriptionCommand;
        _removeTenantSubscriptionCommand = removeTenantSubscriptionCommand;
        _setTenantStatusCommand = setTenantStatusCommand;
        _resetTenantRootPasswordCommand = resetTenantRootPasswordCommand;
    }

    [HttpGet]
    public async Task<ActionResult<Result<PlatformTenantListResponse>>> List(
        [FromQuery] string? name,
        [FromQuery] string? rootEmail,
        [FromQuery] string? rootPhone,
        [FromQuery] string? packageCode,
        [FromQuery] bool? isActive,
        [FromQuery] DateTime? subscriptionStartFrom,
        [FromQuery] DateTime? subscriptionStartTo,
        CancellationToken cancellationToken)
    {
        var filter = new PlatformTenantListFilter(
            name,
            rootEmail,
            rootPhone,
            packageCode,
            isActive,
            subscriptionStartFrom,
            subscriptionStartTo);

        var result = await _getPlatformTenantsQuery.ExecuteAsync(filter, cancellationToken);
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
    public async Task<ActionResult<Result<ImpersonateTenantTicketDto>>> Impersonate(
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

    [HttpPost("{id:int}/subscription/cancel")]
    public async Task<ActionResult<Result<CancelTenantSubscriptionResponse>>> CancelSubscription(
        int id,
        [FromBody] CancelTenantSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _cancelTenantSubscriptionCommand.ExecuteAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorKind == ResultErrorKind.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}/subscriptions/{subscriptionId:int}")]
    public async Task<ActionResult<Result<RemoveTenantSubscriptionResponse>>> RemoveSubscription(
        int id,
        int subscriptionId,
        CancellationToken cancellationToken)
    {
        var result = await _removeTenantSubscriptionCommand.ExecuteAsync(id, subscriptionId, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorKind == ResultErrorKind.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/subscription/link-payment")]
    public async Task<ActionResult<Result<LinkSubscriptionPaymentResponse>>> LinkSubscriptionPayment(
        int id,
        [FromBody] LinkSubscriptionPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _linkSubscriptionPaymentCommand.ExecuteAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorKind == ResultErrorKind.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<Result<bool>>> SetStatus(
        int id,
        [FromBody] SetTenantStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _setTenantStatusCommand.ExecuteAsync(id, request.IsActive, cancellationToken);

        if (!result.IsSuccess && result.ErrorKind == ResultErrorKind.NotFound)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost("{id:int}/root-password/reset")]
    public async Task<ActionResult<Result<ResetTenantRootPasswordResponse>>> ResetRootPassword(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _resetTenantRootPasswordCommand.ExecuteAsync(id, cancellationToken);

        if (!result.IsSuccess && result.ErrorKind == ResultErrorKind.NotFound)
            return NotFound(result);

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<Result<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _deletePlatformTenantCommand.ExecuteAsync(id, cancellationToken);

        if (!result.IsSuccess && result.ErrorKind == ResultErrorKind.NotFound)
            return NotFound(result);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}
