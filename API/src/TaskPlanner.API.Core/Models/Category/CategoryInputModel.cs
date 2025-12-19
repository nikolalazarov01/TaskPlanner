namespace TaskPlanner.API.Core.Models.Category;

/// <summary>
/// Represents the input model used for creating a category
/// </summary>
public class CategoryInputModel
{
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
}