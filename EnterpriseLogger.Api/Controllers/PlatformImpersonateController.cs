using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseLogger.Api.Controllers;

[ApiController]
[Route("api/platform/impersonate")]
public class PlatformImpersonateController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("redirect/{ticket}")]
    public IActionResult RedirectToTenantPanel(string ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket))
            return BadRequest("Ticket gerekli.");

        var tenantPanelBase = Environment.GetEnvironmentVariable("TENANT_PANEL_URL")
            ?? "http://localhost:5173";

        var destination =
            $"{tenantPanelBase.TrimEnd('/')}/impersonate?ticket={Uri.EscapeDataString(ticket.Trim())}";

        return Redirect(destination);
    }
}
