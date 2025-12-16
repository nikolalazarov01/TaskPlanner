namespace TaskPlanner.API.Core.Models.Identity;

public class JwtOptions
{
    public string Issuer { get; set; } = "TaskPlanner";

    public string Audience { get; set; } = "TaskPlannerClients";

    public string Secret { get; set; } = "replace-with-strong-secret";

    public int ExpirationMinutes { get; set; } = 60;
}

