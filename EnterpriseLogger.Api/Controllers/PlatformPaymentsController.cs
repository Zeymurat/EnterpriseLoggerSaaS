using System.Security.Claims;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Platform.Payments.Commands;
using EnterpriseLogger.Application.Features.Platform.Payments.Dtos;
using EnterpriseLogger.Application.Features.Platform.Payments.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/platform/payments")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Policy = AuthPolicies.PlatformAdminOnly)]
public class PlatformPaymentsController : ControllerBase
{
    private readonly GetPlatformPaymentsQuery _getPlatformPaymentsQuery;
    private readonly GetTenantAvailablePaymentsQuery _getTenantAvailablePaymentsQuery;
    private readonly GetTenantPaymentsQuery _getTenantPaymentsQuery;
    private readonly RecordPlatformPaymentCommand _recordPlatformPaymentCommand;
    private readonly ConfirmPlatformPaymentCommand _confirmPlatformPaymentCommand;
    private readonly RejectPlatformPaymentCommand _rejectPlatformPaymentCommand;
    private readonly UpdatePlatformPaymentCommand _updatePlatformPaymentCommand;
    private readonly DeletePlatformPaymentCommand _deletePlatformPaymentCommand;

    public PlatformPaymentsController(
        GetPlatformPaymentsQuery getPlatformPaymentsQuery,
        GetTenantAvailablePaymentsQuery getTenantAvailablePaymentsQuery,
        GetTenantPaymentsQuery getTenantPaymentsQuery,
        RecordPlatformPaymentCommand recordPlatformPaymentCommand,
        ConfirmPlatformPaymentCommand confirmPlatformPaymentCommand,
        RejectPlatformPaymentCommand rejectPlatformPaymentCommand,
        UpdatePlatformPaymentCommand updatePlatformPaymentCommand,
        DeletePlatformPaymentCommand deletePlatformPaymentCommand)
    {
        _getPlatformPaymentsQuery = getPlatformPaymentsQuery;
        _getTenantAvailablePaymentsQuery = getTenantAvailablePaymentsQuery;
        _getTenantPaymentsQuery = getTenantPaymentsQuery;
        _recordPlatformPaymentCommand = recordPlatformPaymentCommand;
        _confirmPlatformPaymentCommand = confirmPlatformPaymentCommand;
        _rejectPlatformPaymentCommand = rejectPlatformPaymentCommand;
        _updatePlatformPaymentCommand = updatePlatformPaymentCommand;
        _deletePlatformPaymentCommand = deletePlatformPaymentCommand;
    }

    [HttpGet]
    public async Task<ActionResult<Result<PlatformPaymentListResponse>>> List(
        [FromQuery] Domain.Enums.PaymentStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await _getPlatformPaymentsQuery.ExecuteAsync(status, cancellationToken);
        return Ok(result);
    }

    [HttpGet("tenant/{tenantId:int}/available")]
    public async Task<ActionResult<Result<PlatformPaymentListResponse>>> ListAvailableForTenant(
        int tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _getTenantAvailablePaymentsQuery.ExecuteAsync(tenantId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("tenant/{tenantId:int}/history")]
    public async Task<ActionResult<Result<PlatformPaymentListResponse>>> ListForTenant(
        int tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _getTenantPaymentsQuery.ExecuteAsync(tenantId, cancellationToken);

        if (!result.IsSuccess && result.ErrorKind == ResultErrorKind.NotFound)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Result<PlatformPaymentDto>>> Record(
        [FromBody] RecordPlatformPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _recordPlatformPaymentCommand.ExecuteAsync(request, cancellationToken);

        if (!result.IsSuccess && result.ErrorKind == ResultErrorKind.NotFound)
            return NotFound(result);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<Result<PlatformPaymentDto>>> Confirm(
        int id,
        [FromBody] ConfirmPlatformPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = ResolvePlatformAdminId();
        var result = await _confirmPlatformPaymentCommand.ExecuteAsync(id, request, adminId, cancellationToken);

        if (!result.IsSuccess && result.ErrorKind == ResultErrorKind.NotFound)
            return NotFound(result);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<Result<PlatformPaymentDto>>> Reject(
        int id,
        [FromBody] RejectPlatformPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _rejectPlatformPaymentCommand.ExecuteAsync(id, request, cancellationToken);

        if (!result.IsSuccess && result.ErrorKind == ResultErrorKind.NotFound)
            return NotFound(result);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Result<PlatformPaymentDto>>> Update(
        int id,
        [FromBody] UpdatePlatformPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updatePlatformPaymentCommand.ExecuteAsync(id, request, cancellationToken);

        if (!result.IsSuccess && result.ErrorKind == ResultErrorKind.NotFound)
            return NotFound(result);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<Result<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _deletePlatformPaymentCommand.ExecuteAsync(id, cancellationToken);

        if (!result.IsSuccess && result.ErrorKind == ResultErrorKind.NotFound)
            return NotFound(result);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    private int? ResolvePlatformAdminId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }
}
