using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Models;

namespace TaskPlanner.API.Core.Interfaces;

public interface IIdentityService
{
    Task<OperationResult> RegisterAsync(RegisterRequest request);

    Task<OperationResult<AuthResponse>> LoginAsync(LoginRequest request);
}

