using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Task;
using TaskPlanner.API.Utilities;
using TaskPlanner.API.Web.Extensions;
using TaskStatus = TaskPlanner.API.Data.Models.TaskStatus;

namespace TaskPlanner.API.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TaskController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IValidator<CreateTaskInputModel> _createTaskRequestValidator;
    private readonly IValidator<UpdateTaskInputModel> _updateTaskRequestValidator;
    private readonly IMapper _mapper;

    public TaskController(ITaskService taskService, IMapper mapper, IValidator<CreateTaskInputModel> createTaskRequestValidator, IValidator<UpdateTaskInputModel> updateTaskRequestValidator)
    {
        _taskService = taskService;
        _mapper = mapper;
        _createTaskRequestValidator = createTaskRequestValidator;
        _updateTaskRequestValidator = updateTaskRequestValidator;
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
        if (task is null) return BadRequest();
    
        if (!ObjectId.TryParse(categoryId, out var categoryObjectId)) return BadRequest();
        
        var validation = await this._createTaskRequestValidator.ValidateAsync(task, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.Errors);
    
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

    /// <summary>
    /// Update - Task
    /// </summary>
    /// <param name="task">The input model used to update an existing task</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled</param>
    /// <returns>
    /// Returns the updated task as <see cref="TaskResponseModel"/> if the request is successful
    /// </returns>
    /// <remarks>
    /// Updates a task for the authenticated user by replacing all mutable fields.
    /// The authenticated user id is extracted from the request context.
    /// The task must exist and belong to the authenticated user.
    /// </remarks>
    /// <response code="200">Returns the updated task</response>
    /// <response code="400">The input is invalid or an error occurred during update</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">The specified task was not found</response>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateTaskInputModel task, CancellationToken cancellationToken)
    {
        if (task is null) return BadRequest();

        var validation = await this._updateTaskRequestValidator.ValidateAsync(task, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        var updateResult = await _taskService.UpdateTask(task, userId, cancellationToken);

        if (!updateResult.Success)
        {
            if (updateResult.Errors.Any(e => e is NotFoundError))
                return NotFound(updateResult.Errors);

            return BadRequest(updateResult.Errors);
        }

        var updatedEntity = updateResult.ResultObject;
        if (updatedEntity is null) return NotFound();

        var response = _mapper.Map<TaskResponseModel>(updatedEntity);
        return Ok(response);
    }

    /// <summary>
    /// Bulk update - Task Status
    /// </summary>
    /// <param name="taskIds">The identifiers of the tasks to update</param>
    /// <param name="status">The new status to assign to all specified tasks</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled</param>
    /// <returns>
    /// Returns the number of updated tasks if the request is successful
    /// </returns>
    /// <remarks>
    /// Updates the status of multiple tasks for the authenticated user in a single operation.
    /// The authenticated user id is extracted from the request context.
    /// All provided task ids must exist and belong to the authenticated user.
    /// </remarks>
    /// <response code="200">Returns the number of updated tasks</response>
    /// <response code="400">The input is invalid or an error occurred during update</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">One or more tasks were not found</response>
    [HttpPatch("update-statuses")]
    public async Task<IActionResult> UpdateStatus([FromQuery] ObjectId[] taskIds, [FromQuery] TaskStatus status, CancellationToken cancellationToken)
    {
        return await UpdateTaskStatusesInternal(taskIds, status, cancellationToken);
    }
    
    [HttpPatch("update-status")]
    public async Task<IActionResult> UpdateStatusOne([FromQuery] ObjectId taskId, [FromQuery] TaskStatus status, CancellationToken cancellationToken)
    {
        return await UpdateTaskStatusesInternal([taskId], status, cancellationToken);
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
    
    private async Task<IActionResult> UpdateTaskStatusesInternal(ObjectId[] taskIds, TaskStatus status, CancellationToken cancellationToken)
    {
        if (taskIds is null || taskIds.Length == 0) return BadRequest();
        if (taskIds.Any(x => x == ObjectId.Empty)) return BadRequest();

        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        // Call the proper service method based on "one vs many"
        OperationResult<long> result = taskIds.Length == 1
            ? await _taskService.ModifyTaskStatus(taskIds[0], userId, status, cancellationToken)
            : await _taskService.ModifyManyTaskStatus(taskIds, userId, status, cancellationToken);

        if (!result.Success)
        {
            if (result.Errors.Any(e => e is NotFoundError))
                return NotFound(result.Errors);

            return BadRequest(result.Errors);
        }

        return Ok(result.ResultObject);
    }
}

