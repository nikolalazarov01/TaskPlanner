using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Models.Identity;

namespace TaskPlanner.API.Core.Interfaces;

/// <summary>
/// Defines the contract for identity-related operations such as user registration and authentication
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Registers a new user using the provided registration data
    /// </summary>
    /// <param name="inputModel">Input model containing registration details such as email, password, and display name</param>
    /// <returns><see cref="OperationResult"/> indicating whether the registration was successful</returns>
    Task<OperationResult> RegisterAsync(RegisterInputModel inputModel);

    /// <summary>
    /// Authenticates a user using the provided login credentials
    /// </summary>
    /// <param name="inputModel">Input model containing login credentials such as email and password</param>
    /// <returns><see cref="OperationResult"/> with result object containing an <see cref="AuthResponse"/> on successful authentication</returns>
    Task<OperationResult<AuthResponse>> LoginAsync(LoginInputModel inputModel);
}