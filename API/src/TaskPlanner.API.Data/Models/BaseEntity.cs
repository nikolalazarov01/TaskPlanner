using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Models;

/// <inheritdoc />
public class BaseEntity : IEntity
{
    /// <inheritdoc />
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }
}