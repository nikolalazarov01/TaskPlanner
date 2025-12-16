using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CategoryController : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromBody] Category category)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpPut]
    public IActionResult Put([FromBody] Category category)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpGet]
    public IActionResult Get([FromQuery] string entityId)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpDelete]
    public IActionResult Delete([FromQuery] string entityId)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}

