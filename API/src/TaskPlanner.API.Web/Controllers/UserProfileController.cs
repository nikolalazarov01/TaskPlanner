using Microsoft.AspNetCore.Mvc;
using TaskPlanner.API.Core.Interfaces;

namespace TaskPlanner.API.Web.Controllers;

/// <summary>
/// Provides endpoints for recomputing and managing user behavioral profiles.
/// </summary>
/// <remarks>
/// This controller is intended primarily for internal/system use (e.g., scheduled jobs),
/// because recomputation may operate on multiple users and can be resource-intensive.
/// If exposed publicly, access should be restricted (e.g., admin-only).
/// </remarks>
[ApiController]
[Route("api/user-profile")]
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileRecomputeQueueService _userProfileRecomputeQueueService;
    private readonly IUserProfileService _userProfileService;

    public UserProfileController(IUserProfileRecomputeQueueService userProfileRecomputeQueueService, IUserProfileService userProfileService)
    {
        _userProfileRecomputeQueueService = userProfileRecomputeQueueService;
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// Recomputes user profiles for all users currently present in the recompute queue.
    /// </summary>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/> used to propagate cancellation requests.
    /// </param>
    /// <returns>
    /// Returns the number of successfully recomputed user profiles.
    /// </returns>
    /// <remarks>
    /// This endpoint reads queued users from <c>UserProfileRecomputeQueue</c> and triggers recomputation for each user.
    /// The recomputation logic is handled by <see cref="IUserProfileService"/>.
    ///
    /// Important:
    /// - This endpoint processes all queue entries currently stored, regardless of lock status or scheduling fields.
    /// - In a production system, this is typically restricted to internal callers (admin/service).
    /// - If multiple workers run concurrently, you should add a "get due & unlocked" query and a lock-claim mechanism.
    /// </remarks>
    /// <response code="200">Returns the number of recomputed users (0 if the queue is empty).</response>
    /// <response code="400">Returned when fetching queued users or recomputation fails.</response>
    /// <response code="401">Returned when the request is unauthorized.</response>
    [HttpPost("recompute")]
    public async Task<IActionResult> Recompute(CancellationToken cancellationToken)
    {
        var getQueuedUsers = await this._userProfileRecomputeQueueService.GetAllQueuedAsync(cancellationToken);

        if (!getQueuedUsers.Success) return BadRequest();

        var queuedUsers = getQueuedUsers.ResultObject;
        if (queuedUsers is null) return NotFound(queuedUsers);
        
        if (queuedUsers.Count == 0)
            return Ok(0);
        
        var userIds = queuedUsers.Select(x => x.UserId).Distinct().ToArray();
        
        var recompute = await this._userProfileService.RecomputeUsersAsync(userIds, cancellationToken);
        if (!recompute.Success) return BadRequest();
        
        var dequeueRecomputedUsers = await this._userProfileRecomputeQueueService.DequeueManyAsync(userIds, cancellationToken);
        if (!dequeueRecomputedUsers.Success) return BadRequest();
        
        return Ok(recompute.ResultObject);
    }
}