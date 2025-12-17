using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Models.Category;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Core.Interfaces;

public interface ICategoryService
{
    Task<OperationResult<Category>> CreateCategory(CategoryInputModel category, ObjectId userId, CancellationToken cancellationToken);
}