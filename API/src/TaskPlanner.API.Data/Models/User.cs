using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TaskPlanner.API.Data.Interfaces;

namespace TaskPlanner.API.Data.Models;

public class User : BaseEntity
{

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("authProvider")]
    public string AuthProvider { get; set; } = "local";

    [BsonElement("externalLogins")]
    public List<string> ExternalLogins { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
}

