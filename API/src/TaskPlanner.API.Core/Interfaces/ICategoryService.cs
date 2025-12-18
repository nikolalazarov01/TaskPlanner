using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Models.Category;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Core.Interfaces;

public interface ICategoryService
{
    Task<OperationResult<Category>> CreateCategory(CategoryInputModel category, ObjectId userId, CancellationToken cancellationToken);

    Task<OperationResult<Category>> UpdateCategory(UpdateCategoryInputModel input, ObjectId userId, CancellationToken cancellationToken);

    Task<OperationResult<Category>> GetCategoryById(string categoryId, ObjectId userId, CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<Category>>> GetCategories(ObjectId userId, CancellationToken cancellationToken);
}