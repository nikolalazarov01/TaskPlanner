using MongoDB.Bson;
using MongoDB.Driver;
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
    
    public async Task<OperationResult<Category>> UpdateCategory(UpdateCategoryInputModel input, ObjectId userId, CancellationToken cancellationToken)
    {
        var result =  new OperationResult<Category>();
        
        if (!ObjectId.TryParse(input.Id, out var categoryId))
        {
            result.AppendError("Invalid category id.");
            return result;
        }

        var updates = new List<UpdateDefinition<Category>>();

        if (!string.IsNullOrWhiteSpace(input.Name))
            updates.Add(Builders<Category>.Update.Set(x => x.Name, input.Name));

        if (input.Color is not null)
            updates.Add(Builders<Category>.Update.Set(x => x.Color, input.Color));

        if (input.SortOrder.HasValue)
            updates.Add(Builders<Category>.Update.Set(x => x.SortOrder, input.SortOrder.Value));

        if (updates.Count == 0)
        {
            result.AppendError("No fields provided for update.");
            return result;
        }

        var update = Builders<Category>.Update.Combine(updates);

        return await _repository.UpdateAsync(
            entity: new Category { Id = categoryId }, // only Id is used by repository
            cancellationToken: cancellationToken,
            update: update);
    }
}