using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Platform.Packages.Commands;
using EnterpriseLogger.Application.Features.Platform.Packages.Dtos;
using EnterpriseLogger.Application.Features.Platform.Packages.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/platform/packages")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Policy = AuthPolicies.PlatformAdminOnly)]
public class PlatformPackagesController : ControllerBase
{
    private readonly GetPlatformPackagesQuery _getPlatformPackagesQuery;
    private readonly UpdatePlatformPackageCommand _updatePlatformPackageCommand;
    private readonly CreatePlatformPackageCommand _createPlatformPackageCommand;
    private readonly DeletePlatformPackageCommand _deletePlatformPackageCommand;

    public PlatformPackagesController(
        GetPlatformPackagesQuery getPlatformPackagesQuery,
        UpdatePlatformPackageCommand updatePlatformPackageCommand,
        CreatePlatformPackageCommand createPlatformPackageCommand,
        DeletePlatformPackageCommand deletePlatformPackageCommand)
    {
        _getPlatformPackagesQuery = getPlatformPackagesQuery;
        _updatePlatformPackageCommand = updatePlatformPackageCommand;
        _createPlatformPackageCommand = createPlatformPackageCommand;
        _deletePlatformPackageCommand = deletePlatformPackageCommand;
    }

    [HttpGet]
    public async Task<ActionResult<Result<PlatformPackageListResponse>>> List(
        CancellationToken cancellationToken)
    {
        var result = await _getPlatformPackagesQuery.ExecuteAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Result<PlatformPackageDto>>> Create(
        [FromBody] CreatePlatformPackageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createPlatformPackageCommand.ExecuteAsync(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Result<PlatformPackageDto>>> Update(
        int id,
        [FromBody] UpdatePlatformPackageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updatePlatformPackageCommand.ExecuteAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorKind == ResultErrorKind.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<Result<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _deletePlatformPackageCommand.ExecuteAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorKind == ResultErrorKind.NotFound)
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }
}
