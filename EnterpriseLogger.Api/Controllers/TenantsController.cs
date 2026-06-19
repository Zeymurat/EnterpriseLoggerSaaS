using EnterpriseLogger.Api.Infrastructure;
using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Tenants.Commands;
using EnterpriseLogger.Application.Features.Tenants.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly CreateTenantCommand _createCommand;
    private readonly RotateTenantApiKeyCommand _rotateApiKeyCommand;

    public TenantsController(
        CreateTenantCommand createCommand,
        RotateTenantApiKeyCommand rotateApiKeyCommand)
    {
        _createCommand = createCommand;
        _rotateApiKeyCommand = rotateApiKeyCommand;
    }

    [HttpPost]
    public async Task<ActionResult<Result<TenantResponseDto>>> Create(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createCommand.ExecuteAsync(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AuthPolicies.ApiKeysRotate)]
    [HttpPost("me/api-key/rotate")]
    public async Task<ActionResult<Result<RotateApiKeyResponseDto>>> RotateApiKey(
        CancellationToken cancellationToken)
    {
        var result = await _rotateApiKeyCommand.ExecuteAsync(cancellationToken);
        return result.ToActionResult();
    }
}
