namespace TaskPlanner.API.Core.Models.Category;

public class UpdateCategoryInputModel
{
    public required string Id { get; set; }

    public string? Name { get; set; }

    public string? Color { get; set; }

    public int? SortOrder { get; set; }
}