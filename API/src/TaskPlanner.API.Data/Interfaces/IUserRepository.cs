using OneBitSoftware.Utilities;
using TaskPlanner.API.Data.Models;
using Task = System.Threading.Tasks.Task;

namespace TaskPlanner.API.Data.Interfaces;

public interface IUserRepository : IBaseRepository
{
    Task<OperationResult<User>> GetByEmailAsync(string email);

    Task<OperationResult> CreateAsync(User user);
}

