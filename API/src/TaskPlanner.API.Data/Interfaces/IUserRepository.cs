using OneBitSoftware.Utilities;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Data.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<OperationResult<User>> GetByEmailAsync(string email);
}

