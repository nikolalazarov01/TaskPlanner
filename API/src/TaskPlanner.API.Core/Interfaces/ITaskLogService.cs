using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Data.Models;
using TaskStatus = TaskPlanner.API.Data.Models.TaskStatus;

namespace TaskPlanner.API.Core.Interfaces;

/// <summary>
/// Defines operations for writing append-only task execution logs.
/// These logs are used for analytics, user profiling, and daily plan generation.
/// </summary>
public interface ITaskLogService
{
    /// <summary>
    /// Logs a task execution event derived from a task status transition.
    /// This method validates task ownership and ensures the event is supported.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who owns the task.</param>
    /// <param name="taskId">The unique identifier of the task.</param>
    /// <param name="previousStatus">The status before the change.</param>
    /// <param name="newStatus">The status after the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="durationSeconds">Optional session duration in seconds.</param>
    /// <param name="metadata">Optional metadata such as device, notes, etc.</param>
    /// <param name="timestampUtc">Optional explicit UTC timestamp for the event.</param>
    /// <returns>The created <see cref="TaskExecutionLog"/> entry.</returns>
    Task<OperationResult<TaskExecutionLog>> LogStatusChange(ObjectId userId, ObjectId taskId, TaskStatus previousStatus, TaskStatus newStatus, CancellationToken cancellationToken, int? durationSeconds = null, BsonDocument? metadata = null, DateTime? timestampUtc = null);

    /// <summary>
    /// Logs an explicit task execution event (start/stop/completed/snoozed/etc.).
    /// This method validates task ownership before writing the log.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who owns the task.</param>
    /// <param name="taskId">The unique identifier of the task.</param>
    /// <param name="eventType">The execution event type to log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="durationSeconds">Optional session duration in seconds.</param>
    /// <param name="metadata">Optional metadata such as device, notes, etc.</param>
    /// <param name="timestampUtc">Optional explicit UTC timestamp for the event.</param>
    /// <returns>The created <see cref="TaskExecutionLog"/> entry.</returns>
    Task<OperationResult<TaskExecutionLog>> LogEvent(ObjectId userId, ObjectId taskId, TaskExecutionEventType eventType, CancellationToken cancellationToken, int? durationSeconds = null, BsonDocument? metadata = null, DateTime? timestampUtc = null);

    /// <summary>
    /// Logs an execution event derived from a status transition for multiple tasks.
    /// Creates one log entry per task after validating ownership for all tasks.
    /// </summary>
    /// <param name="userId">The unique identifier of the user who owns the tasks.</param>
    /// <param name="taskIds">The task identifiers to log for.</param>
    /// <param name="previousStatus">The status before the change.</param>
    /// <param name="newStatus">The status after the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="metadata">Optional metadata applied to each log entry.</param>
    /// <param name="timestampUtc">Optional explicit UTC timestamp used for all log entries.</param>
    /// <returns>The number of log entries created.</returns>
    Task<OperationResult<long>> LogBulkStatusChange(ObjectId userId, ObjectId[] taskIds, TaskStatus previousStatus, TaskStatus newStatus, CancellationToken cancellationToken, BsonDocument? metadata = null, DateTime? timestampUtc = null);
}
