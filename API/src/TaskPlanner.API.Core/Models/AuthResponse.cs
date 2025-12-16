namespace TaskPlanner.API.Core.Models;

public class AuthResponse
{
    public bool Success { get; private set; }

    public string? Token { get; private set; }

    public string? Error { get; private set; }

    public static AuthResponse Failed(string error) => new()
    {
        Success = false,
        Error = error
    };

    public static AuthResponse Succeeded(string token) => new()
    {
        Success = true,
        Token = token
    };
}

