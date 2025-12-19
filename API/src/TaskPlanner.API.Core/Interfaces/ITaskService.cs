using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Models.Task;

namespace TaskPlanner.API.Core.Interfaces;

public interface ITaskService
{
    Task<OperationResult<Data.Models.Task>> CreateTask(CreateTaskInputModel input, ObjectId userId, ObjectId categoryId, CancellationToken cancellationToken);
}