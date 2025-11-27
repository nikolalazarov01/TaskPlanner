using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TaskPlanner.API.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthReportController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("status")]
    public ActionResult<object> CheckStatus()
    {
        return Ok(new
        {
            status = "ok",
            serverTimeUtc = DateTime.UtcNow
        });
    }
}

