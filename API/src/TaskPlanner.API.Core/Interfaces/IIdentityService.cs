using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Models;
using TaskPlanner.API.Core.Models.Identity;

namespace TaskPlanner.API.Core.Interfaces;

public interface IIdentityService
{
    Task<OperationResult> RegisterAsync(RegisterInputModel inputModel);

    Task<OperationResult<AuthResponse>> LoginAsync(LoginInputModel inputModel);
}

