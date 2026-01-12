using MongoDB.Bson;
using MongoDB.Driver;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;
using TaskStatus = TaskPlanner.API.Data.Models.TaskStatus;

namespace TaskPlanner.API.Core.Services;

/// <inheritdoc/>
public class TaskLogService : ITaskLogService
{
    private readonly IBaseRepository<TaskExecutionLog> _taskExecutionLogRepository;
    private readonly IBaseRepository<Data.Models.Task> _taskRepository;

    public TaskLogService(IBaseRepository<TaskExecutionLog> taskExecutionLogRepository, IBaseRepository<Data.Models.Task> taskRepository)
    {
        _taskExecutionLogRepository = taskExecutionLogRepository;
        _taskRepository = taskRepository;
    }

    /// <inheritdoc/>
    public async Task<OperationResult<TaskExecutionLog>> LogStatusChange(ObjectId userId, ObjectId taskId, TaskStatus previousStatus, TaskStatus newStatus, CancellationToken cancellationToken, int? durationSeconds = null, BsonDocument? metadata = null, DateTime? timestampUtc = null)
    {
        var result = new OperationResult<TaskExecutionLog>();

        if (userId == ObjectId.Empty)
            return result.AppendError("Invalid user id.");

        if (taskId == ObjectId.Empty)
            return result.AppendError("Invalid task id.");

        if (durationSeconds.HasValue && durationSeconds.Value < 0)
            return result.AppendError("Invalid duration.");

        // Ensure task exists and is owned by the user (prevents logging against foreign tasks)
        var taskFilter = Builders<Data.Models.Task>.Filter.And(
            Builders<Data.Models.Task>.Filter.Eq(x => x.Id, taskId),
            Builders<Data.Models.Task>.Filter.Eq(x => x.UserId, userId));

        var taskExists = await _taskRepository.AnyAsync(taskFilter, cancellationToken);
        if (!taskExists.Success)
            return result.AppendErrors(taskExists);

        if (!taskExists.ResultObject)
        {
            result.AppendError(new NotFoundError($"Task with id {taskId} not found."));
            return result;
        }

        var eventType = MapStatusChangeToEvent(previousStatus, newStatus);
        if (!eventType.HasValue)
            return result.AppendError("Unsupported status transition for logging.");

        var log = new TaskExecutionLog
        {
            UserId = userId,
            TaskId = taskId,
            Timestamp = timestampUtc ?? DateTime.UtcNow,
            EventType = eventType.Value,
            DurationSeconds = durationSeconds,
            Metadata = metadata
        };

        return await _taskExecutionLogRepository.CreateAsync(log);
    }

    /// <inheritdoc/>
    public async Task<OperationResult<TaskExecutionLog>> LogEvent(ObjectId userId, ObjectId taskId, TaskExecutionEventType eventType, CancellationToken cancellationToken, int? durationSeconds = null, BsonDocument? metadata = null, DateTime? timestampUtc = null)
    {
        var result = new OperationResult<TaskExecutionLog>();

        if (userId == ObjectId.Empty)
            return result.AppendError("Invalid user id.");

        if (taskId == ObjectId.Empty)
            return result.AppendError("Invalid task id.");

        if (durationSeconds.HasValue && durationSeconds.Value < 0)
            return result.AppendError("Invalid duration.");

        // Ensure task exists and is owned by the user
        var taskFilter = Builders<Data.Models.Task>.Filter.And(
            Builders<Data.Models.Task>.Filter.Eq(x => x.Id, taskId),
            Builders<Data.Models.Task>.Filter.Eq(x => x.UserId, userId));

        var taskExists = await _taskRepository.AnyAsync(taskFilter, cancellationToken);
        if (!taskExists.Success)
            return result.AppendErrors(taskExists);

        if (!taskExists.ResultObject)
        {
            result.AppendError(new NotFoundError($"Task with id {taskId} not found."));
            return result;
        }

        var log = new TaskExecutionLog
        {
            UserId = userId,
            TaskId = taskId,
            Timestamp = timestampUtc ?? DateTime.UtcNow,
            EventType = eventType,
            DurationSeconds = durationSeconds,
            Metadata = metadata
        };

        return await _taskExecutionLogRepository.CreateAsync(log);
    }

    /// <inheritdoc/>
    public async Task<OperationResult<long>> LogBulkStatusChange(ObjectId userId, ObjectId[] taskIds, TaskStatus previousStatus, TaskStatus newStatus, CancellationToken cancellationToken, BsonDocument? metadata = null, DateTime? timestampUtc = null)
    {
        var result = new OperationResult<long>();

        if (userId == ObjectId.Empty)
            return result.AppendError("Invalid user id.");

        if (taskIds is null || taskIds.Length == 0)
            return result.AppendError("No tasks provided.");

        if (taskIds.Any(x => x == ObjectId.Empty))
            return result.AppendError("Invalid task id.");

        var eventType = MapStatusChangeToEvent(previousStatus, newStatus);
        if (!eventType.HasValue)
            return result.AppendError("Unsupported status transition for logging.");

        // Ensure all tasks exist and are owned by the user
        var existsFilter = Builders<Data.Models.Task>.Filter.And(
            Builders<Data.Models.Task>.Filter.Eq(x => x.UserId, userId),
            Builders<Data.Models.Task>.Filter.In(x => x.Id, taskIds));

        var existing = await _taskRepository.GetAsync(existsFilter, cancellationToken);
        if (!existing.Success)
            return result.AppendErrors(existing);

        var foundCount = existing.ResultObject?.Count ?? 0;
        if (foundCount != taskIds.Length)
        {
            var existingIds = existing.ResultObject?.Select(x => x.Id).ToHashSet() ?? new HashSet<ObjectId>();
            var missing = taskIds.Where(id => !existingIds.Contains(id)).ToList();

            result.AppendError(new NotFoundError($"Some tasks were not found: {string.Join(", ", missing)}"));
            return result;
        }

        // Insert one log entry per task
        // (Repository base doesn't expose InsertMany; create logs individually.)
        var totalCreated = 0L;
        var ts = timestampUtc ?? DateTime.UtcNow;

        foreach (var taskId in taskIds)
        {
            var log = new TaskExecutionLog
            {
                UserId = userId,
                TaskId = taskId,
                Timestamp = ts,
                EventType = eventType.Value,
                Metadata = metadata
            };

            var created = await _taskExecutionLogRepository.CreateAsync(log);
            if (!created.Success)
                return result.AppendErrors(created);

            totalCreated++;
        }

        return result.WithRelatedObject(totalCreated);
    }

    private static TaskExecutionEventType? MapStatusChangeToEvent(TaskStatus previousStatus, TaskStatus newStatus)
    {
        // Suggested mapping based on your current TaskStatus enum:
        // Todo -> InProgress : Start
        // InProgress -> Todo : Stop
        // InProgress -> Done : Completed
        // Todo -> Done : Completed (allowed if you support quick-complete)
        // Done -> InProgress : Start (if you support reopening; otherwise treat as unsupported)
        // Done -> Todo : Stop (if you support resetting; otherwise treat as unsupported)

        if (previousStatus == TaskStatus.Todo && newStatus == TaskStatus.InProgress)
            return TaskExecutionEventType.Start;

        if (previousStatus == TaskStatus.InProgress && newStatus == TaskStatus.Todo)
            return TaskExecutionEventType.Stop;

        if (previousStatus == TaskStatus.InProgress && newStatus == TaskStatus.Done)
            return TaskExecutionEventType.Completed;

        if (previousStatus == TaskStatus.Todo && newStatus == TaskStatus.Done)
            return TaskExecutionEventType.Completed;

        if (previousStatus == TaskStatus.Done && newStatus == TaskStatus.InProgress)
            return TaskExecutionEventType.Start;

        if (previousStatus == TaskStatus.Done && newStatus == TaskStatus.Todo)
            return TaskExecutionEventType.Stop;

        return null;
    }
}
