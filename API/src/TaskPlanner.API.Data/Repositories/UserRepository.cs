using MongoDB.Driver;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;

namespace TaskPlanner.API.Data.Repositories;

public class UserRepository : MongoDbRepositoryBase<User>, IUserRepository
{
    public UserRepository(IMongoDatabase database)
        : base(database, "users")
    {
    }

    public async Task<OperationResult<User>> GetByEmailAsync(string email)
    {
        var operationResult = new OperationResult<User>();
        
        var user = await Collection
            .Find(x => x.Email == email)
            .FirstOrDefaultAsync();

        return operationResult.WithRelatedObject(user);
    }
}

