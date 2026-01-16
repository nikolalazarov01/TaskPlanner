using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;

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

    /// <summary>
    /// Profiles older than this threshold are considered stale and will be recomputed.
    /// </summary>
    private static readonly TimeSpan FreshnessThreshold = TimeSpan.FromHours(12);
    
    public UserProfileController(IUserProfileRecomputeQueueService userProfileRecomputeQueueService, IUserProfileService userProfileService)
    {
        _userProfileRecomputeQueueService = userProfileRecomputeQueueService;
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// Retrieves a user profile by user id.
    /// If the profile is missing or stale, it triggers a recomputation and returns the updated profile.
    /// </summary>
    /// <param name="userId">The identifier of the user whose profile should be returned.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Returns the user profile.</returns>
    /// <remarks>
    /// A profile is considered stale if it was computed earlier than the configured freshness threshold.
    /// When stale (or missing), the profile is recomputed before returning.
    /// </remarks>
    /// <response code="200">Returns the user profile.</response>
    /// <response code="400">The request is invalid or an error occurred.</response>
    /// <response code="401">The request is unauthorized.</response>
    /// <response code="404">The profile was not found and recomputation failed to create it.</response>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserProfileAsync([FromRoute] ObjectId userId, CancellationToken cancellationToken)
    {
        if (userId == ObjectId.Empty) return BadRequest();

        var getUserProfile = await _userProfileService.GetUserProfile(userId, cancellationToken);

        if (!getUserProfile.Success)
        {
            if (getUserProfile.Errors.Any(e => e is NotFoundError))
            {
                var recomputeMissing = await _userProfileService.RecomputeUserAsync(userId, cancellationToken);
                if (!recomputeMissing.Success) return BadRequest(recomputeMissing.Errors);

                if (recomputeMissing.ResultObject is null) return NotFound();

                return Ok(recomputeMissing.ResultObject);
            }

            return BadRequest(getUserProfile.Errors);
        }

        var profile = getUserProfile.ResultObject;
        if (profile is null) return NotFound();

        var isFresh = (DateTime.UtcNow - profile.ComputedAt) <= FreshnessThreshold;

        if (!isFresh)
        {
            var enqueue = await _userProfileRecomputeQueueService.EnqueueAsync(userId, UserProfileRecomputeReason.TaskEvent, cancellationToken);

            var recompute = await _userProfileService.RecomputeUserAsync(userId, cancellationToken, profile.WindowDays);
            if (!recompute.Success) return BadRequest(recompute.Errors);

            if (recompute.ResultObject is null) return NotFound();

            return Ok(recompute.ResultObject);
        }

        return Ok(profile);
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
    [AllowAnonymous]
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