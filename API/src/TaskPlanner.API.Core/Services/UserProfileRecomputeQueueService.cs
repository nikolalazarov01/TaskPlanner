using MongoDB.Bson;
using MongoDB.Driver;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;

namespace TaskPlanner.API.Core.Services;

/// <inheritdoc/>
public class UserProfileRecomputeQueueService : IUserProfileRecomputeQueueService
{
    private readonly IBaseRepository<UserProfileRecomputeQueue> _queueRepository;

    /// <summary>
    /// Default debounce applied when <c>nextRunAtUtc</c> is not provided.
    /// </summary>
    private static readonly TimeSpan DefaultDebounce = TimeSpan.FromMinutes(5);

    public UserProfileRecomputeQueueService(IBaseRepository<UserProfileRecomputeQueue> queueRepository)
    {
        _queueRepository = queueRepository;
    }

    /// <inheritdoc/>
    public async Task<OperationResult<UserProfileRecomputeQueue>> EnqueueAsync(ObjectId userId, UserProfileRecomputeReason? reason, CancellationToken cancellationToken, DateTime? nextRunAtUtc = null, int? priority = null)
    {
        var result = new OperationResult<UserProfileRecomputeQueue>();

        if (userId == ObjectId.Empty)
            return result.AppendError("Invalid user id.");

        var now = DateTime.UtcNow;
        var desiredNextRun = nextRunAtUtc ?? now.Add(DefaultDebounce);

        // We use ModifyManyAsync with IsUpsert = true because the base repository doesn't expose FindOneAndUpdate with upsert.
        // This upserts by UserId (unique index required).
        //
        // Semantics:
        // - dirty_since: keep earliest (min)
        // - next_run_at: keep earliest (min) to avoid pushing it later
        // - attempts/last_error: reset on enqueue
        // - lock: clear (optional) so a stale lock doesn't block forever
        // - reason: set/update
        // - priority: set/update if provided
        var filter = Builders<UserProfileRecomputeQueue>.Filter.Eq(x => x.UserId, userId);

        var updates = new List<UpdateDefinition<UserProfileRecomputeQueue>>
        {
            Builders<UserProfileRecomputeQueue>.Update
                .SetOnInsert(x => x.UserId, userId)
                .SetOnInsert(x => x.DirtySince, now)
                .SetOnInsert(x => x.Attempts, 0)
        };

        // Keep earliest DirtySince
        updates.Add(Builders<UserProfileRecomputeQueue>.Update.Min(x => x.DirtySince, now));

        // Keep earliest NextRunAt (debounce scheduling)
        updates.Add(Builders<UserProfileRecomputeQueue>.Update.Min(x => x.NextRunAt, desiredNextRun));

        // Reset error state on enqueue (optional but practical)
        updates.Add(Builders<UserProfileRecomputeQueue>.Update.Set(x => x.LastError, null));
        updates.Add(Builders<UserProfileRecomputeQueue>.Update.Set(x => x.Attempts, 0));

        // Clear lock so old locks don't block indefinitely (optional; if you prefer strict locking, remove this)
        updates.Add(Builders<UserProfileRecomputeQueue>.Update.Set(x => x.Lock, null));

        // Update reason
        if (reason.HasValue)
            updates.Add(Builders<UserProfileRecomputeQueue>.Update.Set(x => x.Reason, reason));

        // Update priority only when provided (avoid overwriting existing)
        if (priority.HasValue)
            updates.Add(Builders<UserProfileRecomputeQueue>.Update.Set(x => x.Priority, priority));

        var update = Builders<UserProfileRecomputeQueue>.Update.Combine(updates);

        var upsert = await _queueRepository.ModifyAsync(filter, update, cancellationToken, isUpsert: true);
        if (!upsert.Success)
            return result.AppendErrors(upsert);

        // Return the latest queue entry
        var get = await _queueRepository.GetOneAsync(filter, cancellationToken);
        if (!get.Success)
            return result.AppendErrors(get);

        return result.WithRelatedObject(get.ResultObject);
    }

    /// <inheritdoc/>
    public async Task<OperationResult<bool>> DequeueAsync(ObjectId userId, CancellationToken cancellationToken)
    {
        var result = new OperationResult<bool>();

        if (userId == ObjectId.Empty)
            return result.AppendError("Invalid user id.");

        var filter = Builders<UserProfileRecomputeQueue>.Filter.Eq(x => x.UserId, userId);

        var deleted = await _queueRepository.DeleteOneAsync(filter, cancellationToken);
        if (!deleted.Success)
        {
            // Treat "not found" as not queued (not an error for dequeue)
            if (deleted.Errors.Any(e => e is NotFoundError))
                return result.WithRelatedObject(false);

            return result.AppendErrors(deleted);
        }

        return result.WithRelatedObject(true);
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<long>> DequeueManyAsync(ObjectId[] userIds, CancellationToken cancellationToken)
    {
        var result = new OperationResult<long>();

        if (userIds is null || userIds.Length == 0)
            return result.AppendError("No users provided.");

        if (userIds.Any(x => x == ObjectId.Empty))
            return result.AppendError("Invalid user id.");

        // Delete all queue entries for the provided users
        var filter = Builders<UserProfileRecomputeQueue>.Filter.In(x => x.UserId, userIds);

        var delete = await _queueRepository.DeleteManyAsync(filter, cancellationToken);
        if (!delete.Success)
            return result.AppendErrors(delete);

        return result.WithRelatedObject(delete.ResultObject);
    }

    /// <inheritdoc/>
    public async Task<OperationResult<bool>> IsQueuedAsync(ObjectId userId, CancellationToken cancellationToken)
    {
        var result = new OperationResult<bool>();

        if (userId == ObjectId.Empty)
            return result.AppendError("Invalid user id.");

        var filter = Builders<UserProfileRecomputeQueue>.Filter.Eq(x => x.UserId, userId);

        var any = await _queueRepository.AnyAsync(filter, cancellationToken);
        if (!any.Success)
            return result.AppendErrors(any);

        return result.WithRelatedObject(any.ResultObject);
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<IReadOnlyList<UserProfileRecomputeQueue>>> GetAllQueuedAsync(CancellationToken cancellationToken)
    {
        var result = new OperationResult<IReadOnlyList<UserProfileRecomputeQueue>>();

        var filter = Builders<UserProfileRecomputeQueue>.Filter.Empty;

        var sort = Builders<UserProfileRecomputeQueue>.Sort
            .Ascending(x => x.NextRunAt)
            .Ascending(x => x.DirtySince);

        var get = await _queueRepository.GetAsync(filter, cancellationToken, sort: sort);

        if (!get.Success)
            return result.AppendErrors(get);

        return result.WithRelatedObject(get.ResultObject ?? Array.Empty<UserProfileRecomputeQueue>());
    }
}
