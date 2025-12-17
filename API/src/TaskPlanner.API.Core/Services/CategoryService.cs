using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Category;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities.Constants;

namespace TaskPlanner.API.Core.Services;

public class CategoryService : ICategoryService
{
    private readonly IBaseRepository<Category> _repository;
    
    public CategoryService(IBaseRepository<Category> repository)
    {
        _repository = repository;
    }
    
    public async Task<OperationResult<Category>> CreateCategory(CategoryInputModel category, ObjectId userId, CancellationToken cancellationToken)
    {
        var categoryEntity = new Category()
        {
            Name = category.Name,
            Color = category.Color ?? ApiConstants.CategoryConstants.DefaultColor,
            SortOrder = category.SortOrder ?? ApiConstants.CategoryConstants.DefaultSortOrder,
            UserId = userId,
            CreatedAt = DateTime.Now,
        };

        return await this._repository.CreateAsync(categoryEntity);
    }
}