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
    
    /// <inheritdoc/>
    public async Task<OperationResult<long>> ModifyTaskStatus(ObjectId taskId, ObjectId userId, TaskStatus status, CancellationToken cancellationToken)
    {
        var operationResult = new OperationResult<long>();

        if (userId == ObjectId.Empty)
            return operationResult.AppendError("Invalid user id.");

        if (taskId == ObjectId.Empty)
            return operationResult.AppendError("Invalid task id.");

        var filter = Builders<Data.Models.Task>.Filter.And(
            Builders<Data.Models.Task>.Filter.Eq(x => x.UserId, userId),
            Builders<Data.Models.Task>.Filter.Eq(x => x.Id, taskId));

        // Ensure it exists for this user
        var existing = await _taskRepository.GetOneAsync(filter, cancellationToken);
        if (!existing.Success)
            return operationResult.AppendErrors(existing);

        var update = Builders<Data.Models.Task>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        // Reuse ModifyMany for consistency; the filter matches at most 1 document
        var modified = await _taskRepository.ModifyManyAsync(filter, update, cancellationToken);
        if (!modified.Success)
            return operationResult.AppendErrors(modified);

        return operationResult.WithRelatedObject(modified.ResultObject);
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<long>> ModifyManyTaskStatus(ObjectId[] taskIds, ObjectId userId, TaskStatus status, CancellationToken cancellationToken)
    {
        var operationResult = new OperationResult<long>();

        if (userId == ObjectId.Empty)
            return operationResult.AppendError("Invalid user id.");

        if (taskIds is null || taskIds.Length == 0)
            return operationResult.AppendError("No tasks provided.");

        var filter = Builders<Data.Models.Task>.Filter.And(
            Builders<Data.Models.Task>.Filter.Eq(x => x.UserId, userId),
            Builders<Data.Models.Task>.Filter.In(x => x.Id, taskIds));

        var existing = await _taskRepository.GetAsync(filter, cancellationToken);
        if (!existing.Success)
            return operationResult.AppendErrors(existing);

        var foundCount = existing.ResultObject?.Count ?? 0;
        if (foundCount != taskIds.Length)
        {
            var existingIds = existing.ResultObject?.Select(x => x.Id).ToHashSet() ?? new HashSet<ObjectId>();
            var missing = taskIds.Where(id => !existingIds.Contains(id)).ToList();

            operationResult.AppendError(new NotFoundError($"Some tasks were not found: {string.Join(", ", missing)}"));
            return operationResult;
        }

        var update = Builders<Data.Models.Task>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        var modified = await _taskRepository.ModifyManyAsync(filter, update, cancellationToken);
        if (!modified.Success) return operationResult.AppendErrors(modified);

        return operationResult.WithRelatedObject(modified.ResultObject);
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<Data.Models.Task>> GetTaskById(string id, ObjectId userId, string? categoryId, CancellationToken cancellationToken)
    {
        var result = new OperationResult<Data.Models.Task>();
    
        if (userId == ObjectId.Empty)
            return result.AppendError("Invalid user id.");
    
        if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out var taskObjectId))
            return result.AppendError("Invalid task id.");
    
        ObjectId? categoryObjectId = null;
        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            if (!ObjectId.TryParse(categoryId, out var parsedCategoryId))
                return result.AppendError("Invalid category id.");
    
            categoryObjectId = parsedCategoryId;
        }
    
        var filter = Builders<Data.Models.Task>.Filter.And(
            Builders<Data.Models.Task>.Filter.Eq(x => x.Id, taskObjectId),
            Builders<Data.Models.Task>.Filter.Eq(x => x.UserId, userId));
    
        if (categoryObjectId.HasValue)
        {
            filter = Builders<Data.Models.Task>.Filter.And(
                filter,
                Builders<Data.Models.Task>.Filter.Eq(x => x.CategoryId, categoryObjectId.Value));
        }
    
        return await _taskRepository.GetOneAsync(filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<OperationResult<IReadOnlyList<Data.Models.Task>>> GetTasks(ObjectId userId, string? categoryId, CancellationToken cancellationToken)
    {
        var result = new OperationResult<IReadOnlyList<Data.Models.Task>>();
    
        if (userId == ObjectId.Empty)
            return result.AppendError("Invalid user id.");
    
        ObjectId? categoryObjectId = null;
        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            if (!ObjectId.TryParse(categoryId, out var parsedCategoryId))
                return result.AppendError("Invalid category id.");
    
            categoryObjectId = parsedCategoryId;
        }
    
        var filter = Builders<Data.Models.Task>.Filter.Eq(x => x.UserId, userId);
    
        if (categoryObjectId.HasValue)
        {
            filter = Builders<Data.Models.Task>.Filter.And(
                filter,
                Builders<Data.Models.Task>.Filter.Eq(x => x.CategoryId, categoryObjectId.Value));
        }
    
        // Optional: sort by CreatedAt descending (adjust to your preference)
        var sort = Builders<Data.Models.Task>.Sort.Descending(x => x.CreatedAt);
    
        var get = await _taskRepository.GetAsync(filter, cancellationToken, sort: sort);
    
        if (!get.Success)
            return result.AppendErrors(get);
    
        return result.WithRelatedObject(get.ResultObject ?? Array.Empty<Data.Models.Task>());
    }
}