using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Models.Task;
using TaskStatus = TaskPlanner.API.Data.Models.TaskStatus;

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
    
    /// <summary>
    /// Updates the status of a single task belonging to the specified user.
    /// The task must exist and belong to the given user; otherwise a not-found error is returned.
    /// </summary>
    /// <param name="taskId">The identifier of the task to update.</param>
    /// <param name="userId">The identifier of the user who owns the task.</param>
    /// <param name="status">The new status to assign to the task.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the number of updated tasks (0 or 1) if successful, or validation / not-found errors otherwise.</returns>
    Task<OperationResult<long>> ModifyTaskStatus(ObjectId taskId, ObjectId userId, TaskStatus status, CancellationToken cancellationToken);
    
    /// <summary>
    /// Updates the status of multiple tasks belonging to the specified user in a single bulk operation.
    /// 
    /// All provided task identifiers must exist and belong to the given user; otherwise,
    /// the operation fails with a not-found error and no updates are applied.
    /// </summary>
    /// <param name="taskIds">An array of task identifiers to be updated.</param>
    /// <param name="userId">The identifier of the user who owns the tasks.</param>
    /// <param name="status">The new status to assign to all specified tasks.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate cancellation requests.</param>
    /// <returns>/// An <see cref="OperationResult{T}"/> containing the number of tasks that were updated if the operation succeeds, or validation / not-found errors otherwise.</returns>
    Task<OperationResult<long>> ModifyManyTaskStatus(ObjectId[] taskIds, ObjectId userId, TaskStatus status, CancellationToken cancellationToken);
    
    /// <summary>
    /// Retrieves a single task by its identifier for the specified user.
    /// </summary>
    /// <param name="id">The identifier of the task.</param>
    /// <param name="userId">The identifier of the user who owns the task.</param>
    /// <param name="categoryId">Optional category identifier. When provided, the task must belong to this category.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the task if found, or not-found / validation errors otherwise.</returns>
    Task<OperationResult<Data.Models.Task>> GetTaskById(string id, ObjectId userId, string? categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves tasks belonging to the specified user.
    /// </summary>
    /// <param name="userId">The identifier of the user whose tasks should be retrieved.</param>
    /// <param name="categoryId">Optional category identifier. When provided, only tasks in this category are returned.</param>
    /// <param name="cancellationToken"> The <see cref="CancellationToken"/> used to propagate cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the list of tasks for the user (can be empty), or validation / retrieval errors otherwise.</returns>
    Task<OperationResult<IReadOnlyList<Data.Models.Task>>> GetTasks(ObjectId userId, string? categoryId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Deletes a single task by its identifier for the specified user.
    /// The task must exist and belong to the given user; otherwise a not-found error is returned.
    /// </summary>
    /// <param name="taskId">The identifier of the task to delete.</param>
    /// <param name="userId">The identifier of the user who owns the task.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the deleted task if deletion succeeds, or validation / not-found errors otherwise.</returns>
    Task<OperationResult<Data.Models.Task>> DeleteOne(ObjectId taskId, ObjectId userId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes multiple tasks by their identifiers for the specified user in a single bulk operation.
    /// 
    /// All provided task identifiers must exist and belong to the given user; otherwise,
    /// the operation fails with a not-found error and no deletions are applied.
    /// </summary>
    /// <param name="taskIds">An array of task identifiers to delete.</param>
    /// <param name="userId">The identifier of the user who owns the tasks.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the number of deleted tasks if the operation succeeds, or validation / not-found errors otherwise.</returns>
    Task<OperationResult<long>> DeleteMany(ObjectId[] taskIds, ObjectId userId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Deletes multiple tasks for the specified user in a single bulk operation.
    /// </summary>
    /// <param name="userId">The identifier of the user who owns the tasks.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the number of deleted tasks if the operation succeeds, or validation / not-found errors otherwise.</returns>
    Task<OperationResult<long>> DeleteMany(ObjectId userId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes all tasks in the specified category for the given user.
    /// 
    /// The category must exist and belong to the given user; otherwise a not-found error is returned.
    /// </summary>
    /// <param name="categoryId">The identifier of the category whose tasks should be deleted.</param>
    /// <param name="userId">The identifier of the user who owns the category and tasks.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the number of deleted tasks if the operation succeeds, or validation / not-found errors otherwise.</returns>
    Task<OperationResult<long>> DeleteByCategoryId(ObjectId categoryId, ObjectId userId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes all tasks in the specified category for the given user.
    /// 
    /// The category must exist and belong to the given user; otherwise a not-found error is returned.
    /// </summary>
    /// <param name="categoryIds">The identifiers of the categories whose tasks should be deleted.</param>
    /// <param name="userId">The identifier of the user who owns the category and tasks.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the number of deleted tasks if the operation succeeds, or validation / not-found errors otherwise.</returns>
    Task<OperationResult<long>> DeleteByCategoryId(ObjectId[] categoryIds, ObjectId userId, CancellationToken cancellationToken);
}