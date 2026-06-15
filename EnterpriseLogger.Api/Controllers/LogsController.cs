using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Logs.Commands;
using EnterpriseLogger.Application.Features.Logs.Dtos;
using EnterpriseLogger.Application.Features.Logs.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly CreateLogCommand _createCommand;
    private readonly GetLogsQuery _getLogsQuery;

    public LogsController(CreateLogCommand createCommand, GetLogsQuery getLogsQuery)
    {
        _createCommand = createCommand;
        _getLogsQuery = getLogsQuery;
    }

    [Authorize(Policy = AuthPolicies.LogsRead)]
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<LogResponseDto>>>> Get(
        CancellationToken cancellationToken)
    {
        var result = await _getLogsQuery.ExecuteAsync(cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AuthPolicies.LogsWrite)]
    [HttpPost]
    public async Task<ActionResult<Result<LogResponseDto>>> Create(
        [FromBody] CreateLogRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createCommand.ExecuteAsync(request, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }
}
