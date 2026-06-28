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
    private readonly ExportLogsQuery _exportLogsQuery;

    public LogsController(
        CreateLogCommand createCommand,
        GetLogsQuery getLogsQuery,
        ExportLogsQuery exportLogsQuery)
    {
        _createCommand = createCommand;
        _getLogsQuery = getLogsQuery;
        _exportLogsQuery = exportLogsQuery;
    }

    [Authorize(Policy = AuthPolicies.LogsRead)]
    [HttpGet]
    public async Task<ActionResult<Result<LogListResponseDto>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetLogsQuery.DefaultPageSize,
        [FromQuery] string[]? logLevels = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string[]? applicationNames = null,
        [FromQuery] string[]? httpMethods = null,
        [FromQuery] int[]? statusCodes = null,
        [FromQuery] string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new GetLogsQueryParams(
            page,
            pageSize,
            logLevels,
            search,
            from,
            to,
            applicationNames,
            httpMethods,
            statusCodes,
            correlationId);

        var result = await _getLogsQuery.ExecuteAsync(parameters, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Policy = AuthPolicies.LogsRead)]
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string[]? logLevels = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string[]? applicationNames = null,
        [FromQuery] string[]? httpMethods = null,
        [FromQuery] int[]? statusCodes = null,
        [FromQuery] string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new GetLogsQueryParams(
            1,
            ExportLogsQuery.MaxExportRows,
            logLevels,
            search,
            from,
            to,
            applicationNames,
            httpMethods,
            statusCodes,
            correlationId);

        var result = await _exportLogsQuery.ExecuteAsync(parameters, cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result);

        var export = result.Data!;
        Response.Headers.Append("X-Export-Truncated", export.Truncated ? "true" : "false");
        Response.Headers.Append("X-Export-Total-Matching", export.TotalMatching.ToString());
        Response.Headers.Append("X-Export-Count", export.ExportedCount.ToString());

        return File(export.Content, "text/csv; charset=utf-8", export.FileName);
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
