using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Category;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Core.Services;

public class CategoryService : ICategoryService
{
    private readonly IBaseRepository<Category> _repository;
    
    public CategoryService(IBaseRepository<Category> repository)
    {
        _repository = repository;
    }
    
    public async Task<OperationResult> CreateCategory(CategoryInputModel category, ObjectId userId, CancellationToken cancellationToken)
    {
        //Add constants class with default values
        var categoryEntity = new Category()
        {
            Name = category.Name,
            Color = category.Color ?? "Default",
            SortOrder = category.SortOrder ?? -1,
            UserId = userId,
            CreatedAt = DateTime.Now,
        };

        return await this._repository.CreateAsync(categoryEntity);
    }
}