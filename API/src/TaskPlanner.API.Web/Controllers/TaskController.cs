using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Task;
using TaskPlanner.API.Utilities;
using TaskPlanner.API.Web.Extensions;
using TaskEntity = TaskPlanner.API.Data.Models.Task;

namespace TaskPlanner.API.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TaskController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IMapper _mapper;

    public TaskController(ITaskService taskService, IMapper mapper)
    {
        _taskService = taskService;
        _mapper = mapper;
    }

    /// <summary>
    /// Create - Task
    /// </summary>
    /// <param name="task">The input model used to create a new task</param>
    /// <param name="categoryId">The identifier of the category the task belongs to</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled</param>
    /// <returns>
    /// Returns the created task as <see cref="TaskResponseModel"/> if the request is successful
    /// </returns>
    /// <remarks>
    /// Creates a task for the authenticated user under the specified category.
    /// The authenticated user id is extracted from the request context.
    /// The category must exist and belong to the authenticated user.
    /// </remarks>
    /// <response code="200">Returns the created task</response>
    /// <response code="400">The input is invalid or an error occurred during creation</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">The specified category was not found</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskInputModel task, [FromQuery] string categoryId, CancellationToken cancellationToken)
    {
        if (task is null || string.IsNullOrWhiteSpace(task.Description)) return BadRequest();
        if (string.IsNullOrWhiteSpace(categoryId)) return BadRequest();
    
        if (!ObjectId.TryParse(categoryId, out var categoryObjectId)) return BadRequest();
    
        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();
    
        var createResult = await _taskService.CreateTask(task, userId, categoryObjectId, cancellationToken);
    
        if (!createResult.Success)
        {
            if (createResult.Errors.Any(e => e is NotFoundError))
                return NotFound(createResult.Errors);
    
            return BadRequest(createResult.Errors);
        }
    
        var createdEntity = createResult.ResultObject;
        if (createdEntity is null) return NotFound();
    
        var response = _mapper.Map<TaskResponseModel>(createdEntity);
        return Ok(response);
    }

    [HttpPut]
    public IActionResult Put([FromBody] TaskEntity task)
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

