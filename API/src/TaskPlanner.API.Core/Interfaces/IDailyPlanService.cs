using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Core.Interfaces;

/// <summary>
/// Defines operations for creating and retrieving daily plans for users.
/// </summary>
public interface IDailyPlanService
{
    /// <summary>
    /// Retrieves the active daily plan for a given user and plan date.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="planDate">The local date (in the user's timezone) for which the plan applies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active <see cref="DailyPlan"/> for the specified date.</returns>
    Task<OperationResult<DailyPlan>> GetActiveDailyPlanAsync(ObjectId userId, DateOnly planDate, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new daily plan for a user and plan date.
    /// If an active plan already exists for that date, it is marked as superseded.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="planDate">The local date (in the user's timezone) for which the plan applies.</param>
    /// <param name="inputSnapshot">
    /// Snapshot of tasks and user preferences used to generate the plan.
    /// </param>
    /// <param name="plan">
    /// The generated plan JSON (ordered tasks, optional time blocks, rationales).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="planRunId">
    /// Optional plan run id for traceability. If not provided, a new one is generated.
    /// </param>
    /// <returns>The created <see cref="DailyPlan"/>.</returns>
    Task<OperationResult<DailyPlan>> CreateDailyPlanAsync(ObjectId userId, DateOnly planDate, BsonDocument inputSnapshot, BsonDocument plan, CancellationToken cancellationToken, Guid? planRunId = null);
}
