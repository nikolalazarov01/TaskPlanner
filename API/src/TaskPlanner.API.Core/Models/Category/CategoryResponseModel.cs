namespace TaskPlanner.API.Core.Models.Category;

public class CategoryResponseModel
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string Name { get; set; }

    public string? Color { get; set; }

    public int? SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
}