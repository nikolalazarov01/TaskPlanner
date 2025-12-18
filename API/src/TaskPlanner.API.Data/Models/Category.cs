using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Models;

/// <summary>
/// Represents a category entity stored in the database
/// </summary>
public class Category : IEntity
{
    /// <summary>
    /// The unique identifier of the category document
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId Id { get; set; }

    /// <summary>
    /// The unique identifier of the user who owns the category
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId UserId { get; set; }

    /// <summary>
    /// The name of the category
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional color associated with the category in HEX format
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Optional sort order used to determine the category display order
    /// </summary>
    public int? SortOrder { get; set; }

    /// <summary>
    /// The date and time when the category was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}