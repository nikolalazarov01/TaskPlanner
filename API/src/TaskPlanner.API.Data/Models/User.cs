using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TaskPlanner.API.Data.Attributes;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Models;

/// <summary>
/// Represents a user entity stored in the database
/// </summary>
[MongoCollection("Users")]
public class User : BaseEntity
{
    /// <summary>
    /// The email address of the user, used as a unique identifier for authentication
    /// </summary>
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The hashed password of the user
    /// </summary>
    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the user
    /// </summary>
    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The authentication provider used by the user (e.g. local, google, github)
    /// </summary>
    [BsonElement("authProvider")]
    public string AuthProvider { get; set; } = "local";

    /// <summary>
    /// A list of external authentication providers linked to the user
    /// </summary>
    [BsonElement("externalLogins")]
    public List<string> ExternalLogins { get; set; } = new();

    /// <summary>
    /// The date and time when the user account was created
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The date and time when the user last logged in
    /// </summary>
    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
}