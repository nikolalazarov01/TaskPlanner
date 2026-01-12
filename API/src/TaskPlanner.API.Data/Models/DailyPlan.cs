using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TaskPlanner.API.Data.Attributes;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Models;

/// <summary>
/// Represents a generated daily task plan for a specific user and date.
/// Plans are immutable once generated and versioned via PlanRunId.
/// </summary>
[MongoCollection("DailyPlans")]
public class DailyPlan : IEntity
{
    /// <summary>
    /// The unique identifier of the daily plan document
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }

    /// <summary>
    /// The unique identifier of the user for whom the plan was generated
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId UserId { get; set; }

    /// <summary>
    /// The local calendar date for which this plan applies
    /// </summary>
    public DateOnly PlanDate { get; set; }

    /// <summary>
    /// The UTC timestamp when the plan was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Snapshot of all tasks, preferences, and contextual data
    /// that were provided as input to the planning algorithm or AI
    /// </summary>
    public BsonDocument InputSnapshot { get; set; } = new();

    /// <summary>
    /// The generated plan structure, including ordered tasks,
    /// optional time blocks, and rationales
    /// </summary>
    public BsonDocument Plan { get; set; } = new();

    /// <summary>
    /// The lifecycle status of the plan
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public DailyPlanStatus Status { get; set; } = DailyPlanStatus.Active;

    /// <summary>
    /// Identifier used to version and trace plan generations
    /// </summary>
    public Guid PlanRunId { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Defines the possible states of a daily plan
/// </summary>
public enum DailyPlanStatus
{
    Active,
    Superseded
}
