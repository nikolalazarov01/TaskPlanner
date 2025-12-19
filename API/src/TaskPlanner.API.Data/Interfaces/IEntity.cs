using MongoDB.Bson;

namespace TaskPlanner.API.Data.Interfaces;

/// <summary>
/// A type representing the base entity in the system
/// </summary>
public interface IEntity
{
    /// <summary>
    /// The unique id of the entity
    /// </summary>
    ObjectId Id { get; set; }
}

