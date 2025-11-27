using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;

namespace TestPlanner.Tests.Integration;

internal static class TestTokenFactory
{
    public static string CreateToken()
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("replace-with-strong-secret");

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, ObjectId.GenerateNewId().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, "integration@test.com")
            }),
            Expires = DateTime.UtcNow.AddMinutes(30),
            Issuer = "TaskPlanner",
            Audience = "TaskPlannerClients",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }
}

