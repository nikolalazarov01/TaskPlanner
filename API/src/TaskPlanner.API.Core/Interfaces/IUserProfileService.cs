using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Core.Interfaces;

/// <summary>
/// Defines operations for recomputing and persisting user behavioral profiles.
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Recomputes a user's profile using task execution logs and current task data,
    /// then persists the resulting profile and removes the user from the recompute queue.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to recompute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="windowDays">The rolling window in days used for computations.</param>
    /// <returns>The updated <see cref="UserProfile"/> document.</returns>
    Task<OperationResult<UserProfile>> RecomputeUserAsync(ObjectId userId, CancellationToken cancellationToken, int windowDays = 14);

    /// <summary>
    /// Recomputes profiles for a set of users.
    /// Processing stops on the first failure and returns that failure.
    /// </summary>
    /// <param name="userIds">The users to recompute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="windowDays">The rolling window in days used for computations.</param>
    /// <returns>The number of successfully recomputed users.</returns>
    Task<OperationResult<long>> RecomputeUsersAsync(ObjectId[] userIds, CancellationToken cancellationToken, int windowDays = 14);
}