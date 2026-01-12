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
[Route("api/task")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TaskController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ITaskLogService _taskLogService;
    private readonly IValidator<CreateTaskInputModel> _createTaskRequestValidator;
    private readonly IValidator<UpdateTaskInputModel> _updateTaskRequestValidator;
    private readonly IMapper _mapper;

    public TaskController(ITaskService taskService, ITaskLogService taskLogService, IMapper mapper, IValidator<CreateTaskInputModel> createTaskRequestValidator, IValidator<UpdateTaskInputModel> updateTaskRequestValidator)
    {
        _taskService = taskService;
        _taskLogService = taskLogService;
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
    public async Task<IActionResult> UpdateStatus([FromQuery] ObjectId[] taskIds, [FromQuery] TaskStatus status, [FromQuery] TaskStatus previousStatus, CancellationToken cancellationToken)
    {
        return await UpdateTaskStatusesInternal(taskIds, status, previousStatus, cancellationToken);
    }
    
    /// <summary>
    /// Updates the status of a single task belonging to the authenticated user.
    /// </summary>
    /// <param name="taskId">
    /// The identifier of the task whose status should be updated.
    /// </param>
    /// <param name="status">
    /// The new status to assign to the task.
    /// </param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> used to propagate cancellation requests.
    /// </param>
    /// <returns>
    /// Returns the number of updated tasks (0 or 1) if the request is successful.
    /// </returns>
    /// <remarks>
    /// The authenticated user id is extracted from the request context.
    /// The task must exist and belong to the authenticated user; otherwise, a not-found error is returned.
    /// </remarks>
    /// <response code="200">Returns the number of updated tasks</response>
    /// <response code="400">The input is invalid or an error occurred during the update</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">The specified task was not found</response>
    [HttpPatch("update-status")]
    public async Task<IActionResult> UpdateStatusOne([FromQuery] ObjectId taskId, [FromQuery] TaskStatus status, [FromQuery] TaskStatus previousStatus, CancellationToken cancellationToken)
    {
        return await UpdateTaskStatusesInternal([taskId], status, previousStatus, cancellationToken);
    }

    /// <summary>
    /// Get - Task
    /// </summary>
    /// <param name="id">The identifier of the task</param>
    /// <param name="categoryId">
    /// Optional category identifier. When provided, the task must belong to this category.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled</param>
    /// <returns>Returns a single task as <see cref="TaskResponseModel"/> if found</returns>
    /// <remarks>
    /// Retrieves a single task by id for the authenticated user.
    /// If <paramref name="categoryId"/> is provided, the task must also match that category.
    /// </remarks>
    /// <response code="200">Returns the task</response>
    /// <response code="400">The request is invalid or an error occurred during retrieval</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">The task was not found</response>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOne([FromRoute] string id, [FromQuery] string? categoryId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();
    
        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();
    
        var getResult = await _taskService.GetTaskById(id, userId, categoryId, cancellationToken);
    
        if (!getResult.Success)
        {
            if (getResult.Errors.Any(e => e is NotFoundError)) return NotFound(getResult.Errors);
            return BadRequest(getResult.Errors);
        }
    
        var entity = getResult.ResultObject;
        if (entity is null) return NotFound();
    
        var response = _mapper.Map<TaskResponseModel>(entity);
        return Ok(response);
    }
    
    /// <summary>
    /// Get - Tasks
    /// </summary>
    /// <param name="categoryId">
    /// Optional category identifier. When provided, only tasks in this category are returned.
    /// </param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled</param>
    /// <returns>Returns a list of tasks as <see cref="TaskResponseModel"/> for the authenticated user</returns>
    /// <remarks>
    /// Retrieves tasks belonging to the authenticated user.
    /// If <paramref name="categoryId"/> is provided, tasks are filtered by that category.
    /// </remarks>
    /// <response code="200">Returns the list of tasks (can be empty)</response>
    /// <response code="400">The request is invalid or an error occurred during retrieval</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">No tasks were found (depending on service/repository behavior)</response>
    [HttpGet]
    public async Task<IActionResult> GetMany([FromQuery] string? categoryId, CancellationToken cancellationToken)
    {
        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();
    
        var getResult = await _taskService.GetTasks(userId, categoryId, cancellationToken);
    
        if (!getResult.Success)
        {
            if (getResult.Errors.Any(e => e is NotFoundError)) return NotFound(getResult.Errors);
            return BadRequest(getResult.Errors);
        }
    
        var entities = getResult.ResultObject;
        if (entities is null) return BadRequest();
    
        var response = _mapper.Map<List<TaskResponseModel>>(entities);
        return Ok(response);
    }


        /// <summary>
    /// Delete - Task
    /// </summary>
    /// <param name="id">The identifier of the task to delete.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled.</param>
    /// <returns>
    /// Returns the deleted task as <see cref="TaskResponseModel"/> if the request is successful.
    /// </returns>
    /// <remarks>
    /// Deletes a single task for the authenticated user.
    /// The authenticated user id is extracted from the request context.
    /// The task must exist and belong to the authenticated user.
    /// </remarks>
    /// <response code="200">Returns the deleted task</response>
    /// <response code="400">The request is invalid or an error occurred during deletion</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">The specified task was not found</response>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOne([FromRoute] string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();
        if (!ObjectId.TryParse(id, out var taskId) || taskId == ObjectId.Empty) return BadRequest();

        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        var deleteResult = await _taskService.DeleteOne(taskId, userId, cancellationToken);

        if (!deleteResult.Success)
        {
            if (deleteResult.Errors.Any(e => e is NotFoundError))
                return NotFound(deleteResult.Errors);

            return BadRequest(deleteResult.Errors);
        }

        var deleted = deleteResult.ResultObject;
        if (deleted is null) return NotFound();

        var response = _mapper.Map<TaskResponseModel>(deleted);
        return Ok(response);
    }

    /// <summary>
    /// Delete - Tasks
    /// </summary>
    /// <param name="taskIds">The identifiers of the tasks to delete.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled.</param>
    /// <returns>
    /// Returns the number of deleted tasks if the request is successful.
    /// </returns>
    /// <remarks>
    /// Deletes multiple tasks for the authenticated user in a single operation.
    /// The authenticated user id is extracted from the request context.
    /// All provided task ids must exist and belong to the authenticated user; otherwise no deletions are applied.
    /// </remarks>
    /// <response code="200">Returns the number of deleted tasks</response>
    /// <response code="400">The input is invalid or an error occurred during deletion</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">One or more tasks were not found</response>
    [HttpDelete("many")]
    public async Task<IActionResult> DeleteMany([FromQuery] ObjectId[] taskIds, CancellationToken cancellationToken)
    {
        if (taskIds is null || taskIds.Length == 0) return BadRequest();
        if (taskIds.Any(x => x == ObjectId.Empty)) return BadRequest();

        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        var deleteResult = await _taskService.DeleteMany(taskIds, userId, cancellationToken);

        if (!deleteResult.Success)
        {
            if (deleteResult.Errors.Any(e => e is NotFoundError))
                return NotFound(deleteResult.Errors);

            return BadRequest(deleteResult.Errors);
        }

        return Ok(deleteResult.ResultObject);
    }
    
    /// <summary>
    /// Delete - Tasks for the current user
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled.</param>
    /// <returns>
    /// Returns the number of deleted tasks if the request is successful.
    /// </returns>
    /// <remarks>
    /// Deletes multiple tasks for the authenticated user in a single operation.
    /// The authenticated user id is extracted from the request context.
    /// All provided task ids must exist and belong to the authenticated user; otherwise no deletions are applied.
    /// </remarks>
    /// <response code="200">Returns the number of deleted tasks</response>
    /// <response code="400">The input is invalid or an error occurred during deletion</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">One or more tasks were not found</response>
    [HttpDelete("by-user-id")]
    public async Task<IActionResult> DeleteMany(CancellationToken cancellationToken)
    {
        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        var deleteResult = await _taskService.DeleteMany(userId, cancellationToken);

        if (!deleteResult.Success)
        {
            if (deleteResult.Errors.Any(e => e is NotFoundError))
                return NotFound(deleteResult.Errors);

            return BadRequest(deleteResult.Errors);
        }

        return Ok(deleteResult.ResultObject);
    }

    /// <summary>
    /// Delete - Tasks by Category
    /// </summary>
    /// <param name="categoryId">The identifier of the category whose tasks should be deleted.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled.</param>
    /// <returns>
    /// Returns the number of deleted tasks if the request is successful.
    /// </returns>
    /// <remarks>
    /// Deletes all tasks under the specified category for the authenticated user.
    /// The authenticated user id is extracted from the request context.
    /// The category must exist and belong to the authenticated user.
    /// </remarks>
    /// <response code="200">Returns the number of deleted tasks</response>
    /// <response code="400">The input is invalid or an error occurred during deletion</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">The category was not found</response>
    [HttpDelete("by-category")]
    public async Task<IActionResult> DeleteByCategory([FromQuery] ObjectId categoryId, CancellationToken cancellationToken)
    {
        if (categoryId == ObjectId.Empty) return BadRequest();

        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        var deleteResult = await _taskService.DeleteByCategoryId(categoryId, userId, cancellationToken);

        if (!deleteResult.Success)
        {
            if (deleteResult.Errors.Any(e => e is NotFoundError))
                return NotFound(deleteResult.Errors);

            return BadRequest(deleteResult.Errors);
        }

        return Ok(deleteResult.ResultObject);
    }
    
    private async Task<IActionResult> UpdateTaskStatusesInternal(ObjectId[] taskIds, TaskStatus status, TaskStatus previousStatus, CancellationToken cancellationToken)
    {
        if (taskIds is null || taskIds.Length == 0) return BadRequest();
        if (taskIds.Any(x => x == ObjectId.Empty)) return BadRequest();

        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        OperationResult<long> result = taskIds.Length == 1
            ? await _taskService.ModifyTaskStatus(taskIds[0], userId, status, cancellationToken)
            : await _taskService.ModifyManyTaskStatus(taskIds, userId, status, cancellationToken);

        if (!result.Success)
        {
            if (result.Errors.Any(e => e is NotFoundError))
                return NotFound(result.Errors);

            return BadRequest(result.Errors);
        }

        OperationResult logResult = taskIds.Length == 1
            ? await _taskLogService.LogStatusChange(userId, taskIds[0], previousStatus, status, cancellationToken)
            : await _taskLogService.LogBulkStatusChange(userId, taskIds, previousStatus, status, cancellationToken);


        return Ok(result.ResultObject);
    }
}

