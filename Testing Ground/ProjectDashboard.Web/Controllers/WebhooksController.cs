using Microsoft.AspNetCore.Mvc;

namespace ProjectDashboard.Web.Controllers;

[ApiController]
[Route("api/webhooks-test")]
public class WebhooksController : ControllerBase
{
    [HttpPost]
    public IActionResult Receive([FromBody] object payload)
    {
        return Ok(new { status = "received", at = DateTime.UtcNow });
    }
}
