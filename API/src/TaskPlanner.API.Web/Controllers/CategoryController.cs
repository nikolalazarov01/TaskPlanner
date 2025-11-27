using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public IActionResult Post([FromBody] Category category)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [AllowAnonymous]
    [HttpPut]
    public IActionResult Put([FromBody] Category category)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Get([FromQuery] string entityId)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [AllowAnonymous]
    [HttpDelete]
    public IActionResult Delete([FromQuery] string entityId)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}

