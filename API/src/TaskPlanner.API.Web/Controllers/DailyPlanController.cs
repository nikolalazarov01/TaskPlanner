using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.DailyPlan;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;
using TaskPlanner.API.Web.Extensions;

namespace TaskPlanner.API.Web.Controllers;

/// <summary>
/// Provides endpoints for retrieving and creating daily plans for the authenticated user.
/// </summary>
/// <remarks>
/// Daily plans are generated per user and per local date (in the user's timezone).
/// Creating a new plan for the same date supersedes the existing active plan.
/// </remarks>
[ApiController]
[Route("api/daily-plan")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DailyPlanController : ControllerBase
{
    private readonly IDailyPlanService _dailyPlanService;

    public DailyPlanController(IDailyPlanService dailyPlanService)
    {
        _dailyPlanService = dailyPlanService;
    }

    /// <summary>
    /// Retrieves the active daily plan for the authenticated user and specified plan date.
    /// </summary>
    /// <param name="planDate">
    /// The local date (in the user's timezone) for which the plan should be retrieved.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Returns the active <see cref="DailyPlan"/> for the specified date.</returns>
    /// <remarks>
    /// If no active plan exists for the given date, a not-found response is returned.
    /// </remarks>
    /// <response code="200">Returns the active daily plan.</response>
    /// <response code="400">The request is invalid or an error occurred.</response>
    /// <response code="401">The request is unauthorized.</response>
    /// <response code="404">No active daily plan exists for the specified date.</response>
    [HttpGet]
    public async Task<IActionResult> GetActive([FromQuery] DateOnly planDate, CancellationToken cancellationToken)
    {
        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        var get = await _dailyPlanService.GetActiveDailyPlanAsync(userId, planDate, cancellationToken);

        if (!get.Success)
        {
            if (get.Errors.Any(e => e is NotFoundError))
                return NotFound(get.Errors);

            return BadRequest(get.Errors);
        }

        return Ok(get.ResultObject);
    }

    /// <summary>
    /// Creates a new daily plan for the authenticated user and specified plan date.
    /// </summary>
    /// <param name="planDate">
    /// The local date (in the user's timezone) for which the plan is being created.
    /// </param>
    /// <param name="request">The plan payload containing the input snapshot and the generated plan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Returns the created <see cref="DailyPlan"/>.</returns>
    /// <remarks>
    /// Creating a plan for a date where an active plan already exists will supersede the existing active plan.
    /// This endpoint assumes the caller (e.g., an n8n workflow) provides validated plan JSON.
    /// </remarks>
    /// <response code="200">Returns the created daily plan.</response>
    /// <response code="400">The request is invalid or an error occurred.</response>
    /// <response code="401">The request is unauthorized.</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] DateOnly planDate, [FromBody] CreateDailyPlanRequestModel request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest();
        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        // Convert JSON strings into BsonDocument (keeps DailyPlan schema flexible)
        BsonDocument inputSnapshot;
        BsonDocument plan;

        try
        {
            inputSnapshot = string.IsNullOrWhiteSpace(request.InputSnapshotJson)
                ? new BsonDocument()
                : BsonDocument.Parse(request.InputSnapshotJson);

            plan = string.IsNullOrWhiteSpace(request.PlanJson)
                ? new BsonDocument()
                : BsonDocument.Parse(request.PlanJson);
        }
        catch (Exception ex)
        {
            return BadRequest(new[] { ex.Message });
        }

        var create = await _dailyPlanService.CreateDailyPlanAsync(
            userId,
            planDate,
            inputSnapshot,
            plan,
            cancellationToken,
            request.PlanRunId);

        if (!create.Success)
            return BadRequest(create.Errors);

        return Ok(create.ResultObject);
    }
}
