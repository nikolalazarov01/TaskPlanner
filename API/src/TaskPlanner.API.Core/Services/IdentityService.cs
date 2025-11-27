using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using OneBitSoftware.Utilities;
using OneBitSoftware.Utilities.Errors;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Core.Services;

public class IdentityService : IIdentityService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtOptions _jwtOptions;

    public IdentityService(IUserRepository userRepository, IOptions<JwtOptions> jwtOptions)
    {
        _userRepository = userRepository;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<OperationResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var operationResult = new OperationResult<AuthResponse>();
        
        var existingUserResult = await _userRepository.GetByEmailAsync(request.Email);

        if (!existingUserResult.Success)
        {
            return operationResult.AppendError("User already exists.");
        }

        if (existingUserResult.ResultObject is null)
        {
            return operationResult.AppendError("Something went wrong.");
        }

        var newUser = new User
        {
            Id = ObjectId.GenerateNewId(),
            Email = request.Email,
            DisplayName = request.DisplayName,
            PasswordHash = HashPassword(request.Password),
            AuthProvider = "local",
            CreatedAt = DateTime.UtcNow,
            ExternalLogins = new List<string>()
        };

        var creationResult = await _userRepository.CreateAsync(newUser);
        if (!creationResult.Success) return operationResult.AppendErrors(creationResult);
        
        var token = GenerateJwtToken(newUser);
        return operationResult.WithRelatedObject(AuthResponse.Succeeded(token));
    }

    public async Task<OperationResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var operationResult = new OperationResult<AuthResponse>();
        
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (!user.Success)
        {
            return operationResult.AppendErrors(user);
        }

        if (user.ResultObject is null)
        {
            return operationResult.AppendError("Something went wrong");
        }

        if (!VerifyPassword(request.Password, user.ResultObject.PasswordHash))
        {
            return operationResult.AppendError("Invalid credentials.");
        }

        user.ResultObject.LastLoginAt = DateTime.UtcNow;

        var token = GenerateJwtToken(user.ResultObject);
        return operationResult.WithRelatedObject(AuthResponse.Succeeded(token));
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("displayName", user.DisplayName)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var hashedInput = HashPassword(password);
        return hashedInput.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
    }
}

