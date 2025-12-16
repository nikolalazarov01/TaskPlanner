using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Models.Category;

namespace TaskPlanner.API.Core.Interfaces;

public interface ICategoryService
{
    Task<OperationResult> CreateCategory(CategoryInputModel category, ObjectId userId, CancellationToken cancellationToken);
}