using MongoDB.Driver;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.Tests.Integration;

// This replaces InMemoryUserRepository in your integration tests.
public class MongoUserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public MongoUserRepository(Mongo2GoFixture mongoFixture)
    {
        // Use the test database from the Mongo2Go fixture
        _users = mongoFixture.Database.GetCollection<User>("Users");
    }

    public async Task<OperationResult<User>> GetByEmailAsync(string email)
    {
        var result = new OperationResult<User>();

        var user = await _users
            .Find(u => u.Email == email)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
        
        var allUsers = await _users
            .Find(FilterDefinition<User>.Empty)
            .ToListAsync()
            .ConfigureAwait(false);

        if (user is not null)
        {
            result.WithRelatedObject(user);
        }

        return result;
    }

    public async Task<OperationResult<User>> CreateAsync(User user)
    {
        var result = new OperationResult<User>();

        // Keep the same "User already exists" semantics as your in-memory version
        var existing = await _users
            .Find(u => u.Email == user.Email)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return result.AppendError("User already exists.");
        }

        await _users.InsertOneAsync(user).ConfigureAwait(false);

        return result;
    }
}