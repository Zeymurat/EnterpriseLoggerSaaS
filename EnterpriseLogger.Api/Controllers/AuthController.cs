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

    public AuthController(LoginCommand loginCommand)
    {
        _loginCommand = loginCommand;
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
            if (result.ErrorMessage?.Contains("pasif", StringComparison.OrdinalIgnoreCase) == true)
                return StatusCode(StatusCodes.Status403Forbidden, result);

            if (result.ErrorMessage?.StartsWith("Validasyon", StringComparison.OrdinalIgnoreCase) == true)
                return BadRequest(result);

            return Unauthorized(result);
        }

        return Ok(result);
    }
}
