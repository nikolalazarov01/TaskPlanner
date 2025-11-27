using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Models;

public class Task : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId UserId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId? CategoryId { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime? Deadline { get; set; }

    [BsonRepresentation(BsonType.String)]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [BsonRepresentation(BsonType.String)]
    public TaskStatus Status { get; set; } = TaskStatus.Todo;

    public int? EstimatedMinutes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

public enum TaskPriority
{
    Low,
    Medium,
    High
}

public enum TaskStatus
{
    Todo,
    InProgress,
    Done
}

