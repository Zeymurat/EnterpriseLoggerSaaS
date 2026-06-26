using EnterpriseLogger.Application.Common.Constants;
using EnterpriseLogger.Application.Common.Models;
using EnterpriseLogger.Application.Features.Platform.Audit.Dtos;
using EnterpriseLogger.Application.Features.Platform.Audit.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/platform/audit-logs")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Policy = AuthPolicies.PlatformAdminOnly)]
public class PlatformAuditLogsController : ControllerBase
{
    private readonly GetPlatformAuditLogsQuery _getPlatformAuditLogsQuery;

    public PlatformAuditLogsController(GetPlatformAuditLogsQuery getPlatformAuditLogsQuery)
    {
        _getPlatformAuditLogsQuery = getPlatformAuditLogsQuery;
    }

    [HttpGet]
    public async Task<ActionResult<Result<PlatformAuditLogListResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetPlatformAuditLogsQuery.DefaultPageSize,
        [FromQuery] int? tenantId = null,
        [FromQuery] string? action = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _getPlatformAuditLogsQuery.ExecuteAsync(
            page,
            pageSize,
            tenantId,
            action,
            cancellationToken);

        return Ok(result);
    }
}
