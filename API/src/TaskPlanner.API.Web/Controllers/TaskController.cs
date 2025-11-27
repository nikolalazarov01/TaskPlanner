using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskEntity = TaskPlanner.API.Data.Models.Task;

namespace TaskPlanner.API.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public IActionResult Post([FromBody] TaskEntity task)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [AllowAnonymous]
    [HttpPut]
    public IActionResult Put([FromBody] TaskEntity task)
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

