using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Auth.Commands;
using EnterpriseLogger.Application.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LoginCommand _loginCommand;
    private readonly RefreshSessionCommand _refreshSessionCommand;

    public AuthController(LoginCommand loginCommand, RefreshSessionCommand refreshSessionCommand)
    {
        _loginCommand = loginCommand;
        _refreshSessionCommand = refreshSessionCommand;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<Result<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _loginCommand.ExecuteAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorCode == AuthErrorCodes.AmbiguousTenantContext)
                return BadRequest(result);

            if (result.ErrorMessage?.Contains("pasif", StringComparison.OrdinalIgnoreCase) == true)
                return StatusCode(StatusCodes.Status403Forbidden, result);

            if (result.ErrorMessage?.StartsWith("Validasyon", StringComparison.OrdinalIgnoreCase) == true)
                return BadRequest(result);

            return Unauthorized(result);
        }

        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("refresh")]
    public async Task<ActionResult<Result<LoginResponse>>> Refresh(CancellationToken cancellationToken)
    {
        var result = await _refreshSessionCommand.ExecuteAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("Maksimum oturum", StringComparison.OrdinalIgnoreCase) == true
                || result.ErrorMessage?.Contains("Oturum gerekli", StringComparison.OrdinalIgnoreCase) == true)
                return Unauthorized(result);

            if (result.ErrorMessage?.Contains("pasif", StringComparison.OrdinalIgnoreCase) == true)
                return StatusCode(StatusCodes.Status403Forbidden, result);

            return Unauthorized(result);
        }

        return Ok(result);
    }
}
