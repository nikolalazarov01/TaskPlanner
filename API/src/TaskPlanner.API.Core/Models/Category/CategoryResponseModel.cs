namespace TaskPlanner.API.Core.Models.Category;

/// <summary>
/// Represents the response model returned by the API
/// </summary>
public class CategoryResponseModel
{
    /// <summary>
    /// The unique identifier of the category
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The unique identifier of the user who owns the category
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// The name of the category
    /// </summary>
    public required string Name { get; set; }

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
    public DateTime CreatedAt { get; set; }
}