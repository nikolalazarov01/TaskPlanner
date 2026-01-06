using MongoDB.Bson;
using MongoDB.Driver;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Category;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;
using TaskPlanner.API.Utilities.Constants;

namespace TaskPlanner.API.Core.Services;

/// <inheritdoc/>
public class CategoryService : ICategoryService
{
    private readonly IBaseRepository<Category> _repository;
    
    public CategoryService(IBaseRepository<Category> repository)
    {
        _repository = repository;
    }
    
    /// <inheritdoc/>
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
    
    /// <inheritdoc/>
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

        return await _repository.ModifyAsync(
            entity: new Category { Id = categoryId }, // only Id is used by repository
            cancellationToken: cancellationToken,
            update: update);
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<Category>> GetCategoryById(string categoryId, ObjectId userId, CancellationToken cancellationToken)
    {
        var filter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(x => x.Id, new ObjectId(categoryId)),
            Builders<Category>.Filter.Eq(x => x.UserId, userId)
        );

        return await _repository.GetOneAsync(filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<OperationResult<IReadOnlyList<Category>>> GetCategories(ObjectId userId, CancellationToken cancellationToken)
    {
        var filter = Builders<Category>.Filter.Eq(x => x.UserId, userId);

        var sort = Builders<Category>.Sort.Ascending(x => x.SortOrder).Ascending(x => x.CreatedAt);

        return await _repository.GetAsync(filter: filter, sort: sort, skip: null, limit: null, cancellationToken: cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<Category>> DeleteCategory(ObjectId categoryId, ObjectId userId, CancellationToken cancellationToken)
    {
        var result = new OperationResult<Category>();
        
        if (categoryId == ObjectId.Empty)
            return result.AppendError("Invalid category id.");

        if (userId == ObjectId.Empty)
            return result.AppendError("Invalid user id.");

        var filter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(x => x.Id, categoryId),
            Builders<Category>.Filter.Eq(x => x.UserId, userId)
        );

        return await _repository.DeleteOneAsync(filter, cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<long>> DeleteCategories(ObjectId[] categoryIds, ObjectId userId, CancellationToken cancellationToken)
    {
        var operationResult = new OperationResult<long>();

        if (userId == ObjectId.Empty)
            return operationResult.AppendError("Invalid user id.");

        if (categoryIds.Any(x => x == ObjectId.Empty))
            return operationResult.AppendError("Invalid category id.");

        var filter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(x => x.UserId, userId),
            Builders<Category>.Filter.In(x => x.Id, categoryIds)
        );

        // Ensure all exist for this user (so we keep the "all-or-nothing" semantics)
        var existing = await _repository.GetAsync(filter, cancellationToken);
        if (!existing.Success)
            return operationResult.AppendErrors(existing);

        var foundCount = existing.ResultObject?.Count ?? 0;
        if (foundCount != categoryIds.Length)
        {
            var existingIds = existing.ResultObject?.Select(x => x.Id).ToHashSet() ?? new HashSet<ObjectId>();
            var missing = categoryIds.Where(id => !existingIds.Contains(id)).ToList();

            operationResult.AppendError(new NotFoundError($"Some categories were not found: {string.Join(", ", missing)}"));
            return operationResult;
        }

        var deleted = await _repository.DeleteManyAsync(filter, cancellationToken);
        if (!deleted.Success)
            return operationResult.AppendErrors(deleted);

        return operationResult.WithRelatedObject(deleted.ResultObject);
    }
    
    /// <inheritdoc/>
    public async Task<OperationResult<long>> DeleteCategories(ObjectId userId, CancellationToken cancellationToken)
    {
        var filter = Builders<Category>.Filter.Eq(x => x.UserId, userId);

        return await _repository.DeleteManyAsync(filter, cancellationToken);
    }
}