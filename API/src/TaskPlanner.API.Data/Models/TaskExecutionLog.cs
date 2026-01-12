using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TaskPlanner.API.Data.Attributes;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Models;

/// <summary>
/// Represents a single immutable execution or lifecycle event for a task.
/// This collection is append-only and serves as the source of truth
/// for behavioral analytics and profile computation.
/// </summary>
[MongoCollection("TaskExecutionLogs")]
public class TaskExecutionLog : IEntity
{
    /// <summary>
    /// The unique identifier of the execution log entry
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }

    /// <summary>
    /// The unique identifier of the user who generated the event
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId UserId { get; set; }

    /// <summary>
    /// The unique identifier of the task associated with the event
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId TaskId { get; set; }

    /// <summary>
    /// The UTC timestamp when the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The type of task execution or lifecycle event
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public TaskExecutionEventType EventType { get; set; }

    /// <summary>
    /// Duration of the task execution session in seconds.
    /// Only applicable for completed or stopped sessions.
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Additional contextual data related to the event,
    /// such as device information or user notes
    /// </summary>
    public BsonDocument? Metadata { get; set; }
}

/// <summary>
/// Defines the possible execution and lifecycle events for a task
/// </summary>
public enum TaskExecutionEventType
{
    Start,
    Stop,
    Completed,
    Snoozed,
    MissedDeadline,
    Rescheduled
}
