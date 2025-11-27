using System.Collections.Concurrent;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using Task = System.Threading.Tasks.Task;

namespace TestPlanner.Tests.Integration;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);

    public Task<OperationResult<User>> GetByEmailAsync(string email)
    {
        var result = new OperationResult<User>();
        _users.TryGetValue(email, out var user);

        if (user is not null)
        {
            result.WithRelatedObject(user);
        }

        return Task.FromResult(result);
    }

    public Task<OperationResult> CreateAsync(User user)
    {
        var result = new OperationResult();

        if (_users.TryAdd(user.Email, user))
        {
            return Task.FromResult(result);
        }

        return Task.FromResult(result.AppendError("User already exists."));
    }
}

