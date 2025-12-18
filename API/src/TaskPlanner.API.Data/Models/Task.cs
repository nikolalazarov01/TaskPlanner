using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Models;

/// <summary>
/// Represents a task entity stored in the database
/// </summary>
public class Task : IEntity
{
    /// <summary>
    /// The unique identifier of the task document
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }

    /// <summary>
    /// The unique identifier of the user who owns the task
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId UserId { get; set; }

    /// <summary>
    /// Optional identifier of the category this task belongs to
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId? CategoryId { get; set; }

    /// <summary>
    /// The description or title of the task
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional deadline date and time for completing the task
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// The priority level of the task
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>
    /// The current status of the task
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public TaskStatus Status { get; set; } = TaskStatus.Todo;

    /// <summary>
    /// Optional estimated time in minutes required to complete the task
    /// </summary>
    public int? EstimatedMinutes { get; set; }

    /// <summary>
    /// The date and time when the task was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The date and time when the task was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Defines the priority levels that can be assigned to a task
/// </summary>
public enum TaskPriority
{
    Low,
    Medium,
    High
}

/// <summary>
/// Defines the possible states of a task within its lifecycle
/// </summary>
public enum TaskStatus
{
    Todo,
    InProgress,
    Done
}
