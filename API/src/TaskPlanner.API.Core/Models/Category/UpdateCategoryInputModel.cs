namespace TaskPlanner.API.Core.Models.Category;

/// <summary>
/// Represents the input model used for updating an existing category
/// </summary>
public class UpdateCategoryInputModel
{
    /// <summary>
    /// The unique identifier of the category to be updated
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Optional updated name of the category
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional updated color associated with the category in HEX format
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Optional updated sort order used to determine the category display order
    /// </summary>
    public int? SortOrder { get; set; }
}