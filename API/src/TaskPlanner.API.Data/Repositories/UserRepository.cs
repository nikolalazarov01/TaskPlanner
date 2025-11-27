using MongoDB.Driver;
using OneBitSoftware.Utilities;
using OneBitSoftware.Utilities.Errors;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;
using Task = System.Threading.Tasks.Task;

namespace TaskPlanner.API.Data.Repositories;

public class UserRepository : MongoDbRepositoryBase<User>, IUserRepository
{
    public UserRepository(IMongoDatabase database)
        : base(database, "users")
    {
    }

    public async Task<OperationResult> CreateAsync(User user)
    {
        var operationResult = new OperationResult();
        try
        {
            await Collection.InsertOneAsync(user);
        }
        catch (MongoWriteException e) when (e.WriteError is not null && e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var error = new OperationError(e.WriteError.Message);
            operationResult.AppendError(error);
        }

        return operationResult;
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

