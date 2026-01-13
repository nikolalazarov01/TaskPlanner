using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Core.Interfaces;

/// <summary>
/// Defines operations for managing the user profile recomputation queue.
/// Queue entries are used to debounce and schedule profile recomputation work.
/// </summary>
public interface IUserProfileRecomputeQueueService
{
    /// <summary>
    /// Marks a user as requiring profile recomputation by inserting or updating a queue entry.
    /// If an entry already exists, the method keeps the earliest <see cref="UserProfileRecomputeQueue.DirtySince"/>
    /// and can optionally update the <see cref="UserProfileRecomputeQueue.NextRunAt"/> for debouncing.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to mark for recomputation.</param>
    /// <param name="reason">Optional reason why the user was marked dirty.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="nextRunAtUtc">
    /// Optional earliest UTC time when recomputation should run.
    /// If not provided, the service will pick a sensible default debounce time.
    /// </param>
    /// <param name="priority">Optional priority for recomputation ordering.</param>
    /// <returns>The upserted queue entry.</returns>
    Task<OperationResult<UserProfileRecomputeQueue>> EnqueueAsync(ObjectId userId, UserProfileRecomputeReason? reason, CancellationToken cancellationToken, DateTime? nextRunAtUtc = null, int? priority = null);

    /// <summary>
    /// Removes a user from the recomputation queue (used after successful recomputation).
    /// </summary>
    /// <param name="userId">The unique identifier of the user to remove from the queue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if an entry was deleted; otherwise false.</returns>
    Task<OperationResult<bool>> DequeueAsync(ObjectId userId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Removes multiple users from the recomputation queue.
    /// </summary>
    /// <param name="userIds">The user identifiers to remove from the queue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of deleted queue entries.</returns>
    Task<OperationResult<long>> DequeueManyAsync(ObjectId[] userIds, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether a user currently has a queued recomputation entry.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a queue entry exists; otherwise false.</returns>
    Task<OperationResult<bool>> IsQueuedAsync(ObjectId userId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Retrieves all queued users that currently require profile recomputation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of queued recomputation entries.</returns>
    Task<OperationResult<IReadOnlyList<UserProfileRecomputeQueue>>> GetAllQueuedAsync(CancellationToken cancellationToken);
}
