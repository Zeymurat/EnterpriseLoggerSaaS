using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Platform.Auth.Commands;
using EnterpriseLogger.Application.Features.Platform.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/platform/auth")]
public class PlatformAuthController : ControllerBase
{
    private readonly PlatformLoginCommand _loginCommand;

    public PlatformAuthController(PlatformLoginCommand loginCommand)
    {
        _loginCommand = loginCommand;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<Result<PlatformLoginResponse>>> Login(
        [FromBody] PlatformLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _loginCommand.ExecuteAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorKind == ResultErrorKind.TooManyRequests)
            {
                if (result.RetryAfterSeconds.HasValue)
                    Response.Headers.RetryAfter = result.RetryAfterSeconds.Value.ToString();

                return StatusCode(StatusCodes.Status429TooManyRequests, result);
            }

            if (result.ErrorMessage?.StartsWith("Validasyon", StringComparison.OrdinalIgnoreCase) == true)
                return BadRequest(result);

            return Unauthorized(result);
        }

        return Ok(result);
    }

    [Authorize(Policy = AuthPolicies.PlatformAdminOnly)]
    [HttpPost("refresh")]
    public async Task<ActionResult<Result<PlatformLoginResponse>>> Refresh(
        [FromServices] PlatformRefreshSessionCommand refreshCommand,
        CancellationToken cancellationToken)
    {
        var result = await refreshCommand.ExecuteAsync(cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(result);

        return Ok(result);
    }
}
