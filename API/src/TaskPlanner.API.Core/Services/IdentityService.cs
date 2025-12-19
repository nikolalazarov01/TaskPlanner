using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Identity;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Core.Services;

/// <inheritdoc/>
public class IdentityService : IIdentityService
{
    private readonly IBaseRepository<User> _userRepository;
    private readonly JwtOptions _jwtOptions;

    public IdentityService(IBaseRepository<User> userRepository, IOptions<JwtOptions> jwtOptions)
    {
        _userRepository = userRepository;
        _jwtOptions = jwtOptions.Value;
    }

    /// <inheritdoc/>
    public async Task<OperationResult> RegisterAsync(RegisterInputModel inputModel)
    {
        var operationResult = new OperationResult();
        
        var filter = Builders<User>.Filter.Eq(u => u.Email, inputModel.Email);

        var existingUserResult = await _userRepository.GetOneAsync(filter, CancellationToken.None);

        if (existingUserResult.Success || existingUserResult.ResultObject is not null)
        {
            return operationResult.AppendError("User already exists.");
        }

        var newUser = new User
        {
            Id = ObjectId.GenerateNewId(),
            Email = inputModel.Email,
            DisplayName = inputModel.DisplayName,
            PasswordHash = HashPassword(inputModel.Password),
            AuthProvider = "local",
            CreatedAt = DateTime.UtcNow,
            ExternalLogins = new List<string>()
        };

        var creationResult = await _userRepository.CreateAsync(newUser);
        if (!creationResult.Success) return operationResult.AppendErrors(creationResult);

        return operationResult;
    }

    /// <inheritdoc/>
    public async Task<OperationResult<AuthResponse>> LoginAsync(LoginInputModel inputModel)
    {
        var operationResult = new OperationResult<AuthResponse>();
        
        var filter = Builders<User>.Filter.Eq(u => u.Email, inputModel.Email);

        var getUser = await _userRepository.GetOneAsync(filter, CancellationToken.None);
        if (!getUser.Success)
        {
            return operationResult.AppendErrors(getUser);
        }

        if (getUser.ResultObject is null)
        {
            return operationResult.AppendError("Something went wrong");
        }

        if (!VerifyPassword(inputModel.Password, getUser.ResultObject.PasswordHash))
        {
            return operationResult.AppendError("Invalid credentials.");
        }

        getUser.ResultObject.LastLoginAt = DateTime.UtcNow;

        var token = GenerateJwtToken(getUser.ResultObject);
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

