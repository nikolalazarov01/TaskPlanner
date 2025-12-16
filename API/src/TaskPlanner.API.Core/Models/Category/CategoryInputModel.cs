namespace TaskPlanner.API.Core.Models.Category;

public class CategoryInputModel
{
    public required string Name { get; set; }

    public string? Color { get; set; }

    public int? SortOrder { get; set; }
}