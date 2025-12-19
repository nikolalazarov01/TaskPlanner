using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Models.Task;

namespace TaskPlanner.API.Core.Interfaces;

/// <summary>
/// Defines task-related business operations
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Creates a new task for the specified user under the given category
    /// </summary>
    /// <param name="input">
    /// The input model containing task data such as description, priority,
    /// optional deadline, and optional estimated time
    /// </param>
    /// <param name="userId">The identifier of the user creating the task</param>
    /// <param name="categoryId">The identifier of the category the task belongs to</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate cancellation requests</param>
    /// <returns>An <see cref="OperationResult"/> containing the created task entity if successful</returns>
    Task<OperationResult<Data.Models.Task>> CreateTask(CreateTaskInputModel input, ObjectId userId, ObjectId categoryId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Updates an existing task by replacing all of its mutable fields
    /// </summary>
    /// <param name="input">The input model containing the updated task data. All properties represent the desired final state of the task.</param>
    /// <param name="userId">The identifier of the user attempting to update the task. The task must belong to this user.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate cancellation requests</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the updated task entity if the update succeeds, or validation / not-found errors otherwise</returns>
    Task<OperationResult<Data.Models.Task>> UpdateTask(UpdateTaskInputModel input, ObjectId userId, CancellationToken cancellationToken);
}