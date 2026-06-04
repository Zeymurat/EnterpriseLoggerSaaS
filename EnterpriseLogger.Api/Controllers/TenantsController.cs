using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Tenants.Commands;
using EnterpriseLogger.Application.Features.Tenants.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly CreateTenantCommand _command;

    public TenantsController(CreateTenantCommand command)
    {
        _command = command;
    }

    [HttpPost]
    public async Task<ActionResult<Result<TenantResponseDto>>> Create(
        [FromBody] CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _command.ExecuteAsync(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}
