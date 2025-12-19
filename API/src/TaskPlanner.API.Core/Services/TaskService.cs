using MongoDB.Bson;
using MongoDB.Driver;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Task;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskStatus = TaskPlanner.API.Data.Models.TaskStatus;

namespace TaskPlanner.API.Core.Services;

public class TaskService : ITaskService
{
    private readonly IBaseRepository<Data.Models.Task> _taskRepository;
    private readonly IBaseRepository<Category> _categoryRepository;

    public TaskService(IBaseRepository<Data.Models.Task> taskRepository, IBaseRepository<Category> categoryRepository)
    {
        _taskRepository = taskRepository;
        _categoryRepository = categoryRepository;
    }

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
}