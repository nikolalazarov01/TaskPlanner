using TaskPlanner.API.Data.Models;
using TaskStatus = System.Threading.Tasks.TaskStatus;

namespace TaskPlanner.API.Core.Models.Task;

public class TaskResponseModel
{
    public required string Id { get; set; }

    public required string UserId { get; set; }

    public required string CategoryId { get; set; }
    
    public required string Description { get; set; }

    public DateTime? Deadline { get; set; }

    public TaskPriority? Priority { get; set; }

    public TaskStatus? Status { get; set; }

    public int? EstimatedMinutes { get; set; }
}