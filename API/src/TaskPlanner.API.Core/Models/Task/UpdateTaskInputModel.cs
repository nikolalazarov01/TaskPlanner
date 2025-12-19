using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Core.Models.Task;

public class UpdateTaskInputModel
{
    public required string Id { get; set; }
    
    public required string CategoryId { get; set; }
    
    public required string Description { get; set; }

    public DateTime? Deadline { get; set; }

    public TaskPriority? Priority { get; set; }

    public Data.Models.TaskStatus Status { get; set; }

    public int? EstimatedMinutes { get; set; }
}