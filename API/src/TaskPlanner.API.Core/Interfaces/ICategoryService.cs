using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Models.Category;
using TaskPlanner.API.Data.Models;

namespace TaskPlanner.API.Core.Interfaces;

/// <summary>
/// Defines the contract for category-related business operations
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Creates a new category for the specified user
    /// </summary>
    /// <param name="category">Input model containing category data</param>
    /// <param name="userId">Identifier of the user owning the category</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <returns><see cref="OperationResult"/> with result object containing the created category</returns>
    Task<OperationResult<Category>> CreateCategory(CategoryInputModel category, ObjectId userId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing category belonging to the specified user
    /// </summary>
    /// <param name="input">Input model containing updated category fields</param>
    /// <param name="userId">Identifier of the user owning the category</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <returns><see cref="OperationResult"/> with result object containing the updated category</returns>
    Task<OperationResult<Category>> UpdateCategory(UpdateCategoryInputModel input, ObjectId userId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a single category by its identifier for the specified user
    /// </summary>
    /// <param name="categoryId">Identifier of the category</param>
    /// <param name="userId">Identifier of the user owning the category</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <returns><see cref="OperationResult"/> with result object containing the retrieved category</returns>
    Task<OperationResult<Category>> GetCategoryById(string categoryId, ObjectId userId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all categories belonging to the specified user
    /// </summary>
    /// <param name="userId">Identifier of the user owning the categories</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <returns><see cref="OperationResult"/> with result object containing the retrieved categories</returns>
    Task<OperationResult<IReadOnlyList<Category>>> GetCategories(ObjectId userId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a single category belonging to the specified user
    /// </summary>
    /// <param name="categoryId">Identifier of the category</param>
    /// <param name="userId">Identifier of the user owning the category</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <returns><see cref="OperationResult"/> with result object containing the deleted category</returns>
    Task<OperationResult<Category>> DeleteCategory(ObjectId categoryId, ObjectId userId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes all categories belonging to the specified user
    /// </summary>
    /// <param name="categoryIds">Identifiers of the categories</param>
    /// <param name="userId">Identifier of the user owning the categories</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <returns><see cref="OperationResult"/> with result object containing the count of deleted categories</returns>
    Task<OperationResult<long>> DeleteCategories(ObjectId[] categoryIds, ObjectId userId, CancellationToken cancellationToken);
    
    /// <summary>
    /// Deletes all categories belonging to the specified user
    /// </summary>
    /// <param name="userId">Identifier of the user owning the categories</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <returns><see cref="OperationResult"/> with result object containing the count of deleted categories</returns>
    Task<OperationResult<long>> DeleteCategories(ObjectId userId, CancellationToken cancellationToken);
}
