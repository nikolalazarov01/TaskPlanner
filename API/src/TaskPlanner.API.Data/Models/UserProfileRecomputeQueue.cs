using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TaskPlanner.API.Data.Attributes;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Models;

/// <summary>
/// Represents a queued request to recompute a user's behavioral profile.
/// Used to debounce, schedule, and safely process recomputations.
/// </summary>
[MongoCollection("UserProfileRecomputeQueue")]
public class UserProfileRecomputeQueue : IEntity
{
    /// <summary>
    /// The unique identifier of the queue entry
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }

    /// <summary>
    /// The unique identifier of the user requiring recomputation
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId UserId { get; set; }

    /// <summary>
    /// The UTC timestamp indicating when the profile became outdated
    /// </summary>
    public DateTime DirtySince { get; set; }

    /// <summary>
    /// Optional reason why the profile was marked dirty
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public UserProfileRecomputeReason? Reason { get; set; }

    /// <summary>
    /// Optional priority used to influence recomputation order
    /// </summary>
    public int? Priority { get; set; }

    /// <summary>
    /// Lock information used to prevent concurrent recomputation
    /// </summary>
    public RecomputeLock? Lock { get; set; }

    /// <summary>
    /// Number of recomputation attempts made for this entry
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// The last error encountered during recomputation, if any
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// The earliest UTC time at which this entry should be processed
    /// </summary>
    public DateTime? NextRunAt { get; set; }
}

/// <summary>
/// Represents a temporary lock for a recomputation queue entry
/// </summary>
public class RecomputeLock
{
    /// <summary>
    /// Identifier of the worker currently processing the entry
    /// </summary>
    public string LockedBy { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp after which the lock expires
    /// </summary>
    public DateTime LockedUntil { get; set; }
}

/// <summary>
/// Defines the reasons a user profile may require recomputation
/// </summary>
public enum UserProfileRecomputeReason
{
    TaskEvent,
    TimezoneChange,
    PreferenceChange
}
