using MongoDB.Bson;
using MongoDB.Driver;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Task;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;
using TaskStatus = TaskPlanner.API.Data.Models.TaskStatus;

namespace TaskPlanner.API.Core.Services;

/// <inheritdoc/>
public class TaskService : ITaskService
{
    private readonly IBaseRepository<Data.Models.Task> _taskRepository;
    private readonly IBaseRepository<Category> _categoryRepository;

    public TaskService(IBaseRepository<Data.Models.Task> taskRepository, IBaseRepository<Category> categoryRepository)
    {
        _taskRepository = taskRepository;
        _categoryRepository = categoryRepository;
    }

    /// <inheritdoc/>
    public async Task<OperationResult<Data.Models.Task>> CreateTask(CreateTaskInputModel input, ObjectId userId, ObjectId categoryId, CancellationToken cancellationToken)
    {
        var operationResult = new OperationResult<Data.Models.Task>();

        if (userId == ObjectId.Empty) return operationResult.AppendError("Invalid user id.");
        if (categoryId == ObjectId.Empty) return operationResult.AppendError("Invalid category id.");
        
        var filter = Builders<Category>.Filter.And(Builders<Category>.Filter.Eq(x => x.Id, categoryId), Builders<Category>.Filter.Eq(x => x.UserId, userId));

        var categoryExists = await this._categoryRepository.AnyAsync(filter, cancellationToken);
        if (!categoryExists.Success) return operationResult.AppendErrors(categoryExists);
        
        if (!categoryExists.ResultObject) return operationResult.AppendError($"Category with id {categoryId} not found.");

        var entity = new Data.Models.Task
        {
            UserId = userId,
            CategoryId = categoryId,
            Description = input.Description,
            Deadline = input.Deadline,
            Priority = input.Priority ?? TaskPriority.Medium,
            Status = input.Status ?? TaskStatus.Todo,
            EstimatedMinutes = input.EstimatedMinutes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _taskRepository.CreateAsync(entity);
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<Data.Models.Task>> UpdateTask(UpdateTaskInputModel input, ObjectId userId, CancellationToken cancellationToken)
    {
        var operationResult = new OperationResult<Data.Models.Task>();

        if (userId == ObjectId.Empty) return operationResult.AppendError("Invalid user id.");

        var categoryId = new ObjectId(input.CategoryId);

        // Ensure category exists for this user (same rule as Create)
        var categoryFilter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(x => x.Id, categoryId),
            Builders<Category>.Filter.Eq(x => x.UserId, userId));

        var categoryExists = await this._categoryRepository.AnyAsync(categoryFilter, cancellationToken);
        if (!categoryExists.Success) return operationResult.AppendErrors(categoryExists);

        if (!categoryExists.ResultObject)
        {
            operationResult.AppendError(new NotFoundError($"Category with id {categoryId} not found."));
            return operationResult;
        }

        // Fetch existing task (to preserve fields like CreatedAt, and to ensure ownership)
        var getFilter = Builders<Data.Models.Task>.Filter.And(
            Builders<Data.Models.Task>.Filter.Eq(x => x.Id, new ObjectId(input.Id)),
            Builders<Data.Models.Task>.Filter.Eq(x => x.UserId, userId));

        var existingTask = await this._taskRepository.GetOneAsync(getFilter, cancellationToken);
        if (!existingTask.Success) return operationResult.AppendErrors(existingTask);

        var existing = existingTask.ResultObject;
        if (existing is null) return operationResult.AppendError("Entity not found.");
        
        if (existing.CategoryId != categoryId) return operationResult.AppendError($"Original task with id {input.Id} is not with the given category id {input.CategoryId}.");

        // Replace entire entity while preserving immutable/system-managed fields
        var updatedEntity = new Data.Models.Task
        {
            Id = existing.Id,
            UserId = existing.UserId,
            CategoryId = categoryId,

            Description = input.Description,
            Deadline = input.Deadline,
            Priority = input.Priority ?? TaskPriority.Medium,
            Status = input.Status,
            EstimatedMinutes = input.EstimatedMinutes,

            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        return await this._taskRepository.UpdateAsync(updatedEntity, cancellationToken);
    }
}