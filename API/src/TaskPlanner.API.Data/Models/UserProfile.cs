using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TaskPlanner.API.Data.Attributes;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Models;

/// <summary>
/// Represents a computed behavioral profile for a user,
/// derived from task execution logs over a rolling time window.
/// </summary>
[MongoCollection("UserProfiles")]
public class UserProfile : IEntity
{
    /// <summary>
    /// The unique identifier of the user profile document
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }

    /// <summary>
    /// The unique identifier of the user this profile belongs to
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId UserId { get; set; }

    /// <summary>
    /// The UTC timestamp when the profile was last computed
    /// </summary>
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The size of the rolling window, in days,
    /// used to compute the profile metrics
    /// </summary>
    public int WindowDays { get; set; } = 14;

    /// <summary>
    /// The computed behavioral metrics and preferences
    /// derived from task execution data
    /// </summary>
    public BsonDocument Profile { get; set; } = new();
}