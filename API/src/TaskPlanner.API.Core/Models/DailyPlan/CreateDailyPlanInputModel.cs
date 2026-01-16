namespace TaskPlanner.API.Core.Models.DailyPlan;


/// <summary>
/// Represents the request body required to create a daily plan.
/// </summary>
public class CreateDailyPlanRequestModel
{
    /// <summary>
    /// JSON string containing the input snapshot used to generate the plan.
    /// </summary>
    public string InputSnapshotJson { get; set; } = string.Empty;

    /// <summary>
    /// JSON string containing the generated plan structure.
    /// </summary>
    public string PlanJson { get; set; } = string.Empty;

    /// <summary>
    /// Optional plan run identifier used for traceability across systems.
    /// If not provided, the service generates a new identifier.
    /// </summary>
    public Guid? PlanRunId { get; set; }
}