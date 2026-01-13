using MongoDB.Driver;
using OneBitSoftware.Utilities;

namespace TaskPlanner.API.Data.Interfaces;

/// <summary>
/// The base for the repository class
/// </summary>
/// <typeparam name="TEntity">Type parameter, representing the Entity</typeparam>
public interface IBaseRepository<TEntity> where TEntity : IEntity
{
    /// <summary>
    /// A method, used for creating a single entity
    /// </summary>
    /// <param name="entity">The entity, being created</param>
    /// <returns><see cref="OperationResult"/>With result object containing the created entity</returns>
    Task<OperationResult<TEntity>> CreateAsync(TEntity entity);
    
    /// <summary>
    /// A method, used to update (replace) an entire entity in the database
    /// </summary>
    /// <param name="entity">The entity containing the updated state</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <returns>
    /// <see cref="OperationResult"/> with result object containing the updated entity
    /// </returns>
    Task<OperationResult<TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken);
    
    /// <summary>
    /// A method, used for updating many entities using the provided update definition
    /// </summary>
    /// <param name="filter">A definition for the filters, which will be used to match the entities</param>
    /// <param name="update">Update definitions, stating which properties should be modified</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <returns><see cref="OperationResult"/>With result object containing count of the modified entities</returns>
    Task<OperationResult<long>> UpdateManyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, CancellationToken cancellationToken);

    
    /// <summary>
    /// A method, used for modifying an entity
    /// </summary>
    /// <param name="entity">The entity, being modified</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <param name="update">Update definitions, stating which properties should be modified</param>
    /// <param name="isUpsert">A flag indicating whether to insert a new entity if no matches are found</param>
    /// <returns><see cref="OperationResult"/>With result object containing the modified entity</returns>
    Task<OperationResult<TEntity>> ModifyAsync(TEntity entity, CancellationToken cancellationToken, UpdateDefinition<TEntity> update = null, bool isUpsert = false);

    /// <summary>
    /// Applies an update to a single entity matching the provided filter and returns the updated document.
    /// Supports optional upsert behavior.
    /// </summary>
    /// <param name="filter">The filter used to select the entity to update.</param>
    /// <param name="update">The update definition to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="isUpsert">
    /// If true, inserts a new entity when no document matches the filter.
    /// </param>
    /// <returns>The updated (or inserted) entity.</returns>
    Task<OperationResult<TEntity>> ModifyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, CancellationToken cancellationToken, bool isUpsert = false);
    
    /// <summary>
    /// A method, used for modifying many entities using the provided update definition
    /// </summary>
    /// <param name="filter">A definition for the filters, which will be used to match the entities</param>
    /// <param name="update">Update definitions, stating which properties should be modified</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <param name="isUpsert">A flag indicating whether to insert a new entity if no matches are found</param>
    /// <returns><see cref="OperationResult"/>With result object containing count of the modified entities</returns>
    Task<OperationResult<long>> ModifyManyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, CancellationToken cancellationToken, bool isUpsert = false);

    
    /// <summary>
    /// A method, used for retrieving one or more entities from the database
    /// </summary>
    /// <param name="filter">A definition for the filters, which will be used to get the entities</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <param name="sort">A definition, defining the sorting</param>
    /// <param name="skip">Skip definition, used for pagination</param>
    /// <param name="limit">Limit definition, used for pagination</param>
    /// <returns><see cref="OperationResult"/>With result object containing the retrieved entities</returns>
    Task<OperationResult<IReadOnlyList<TEntity>>> GetAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken, SortDefinition<TEntity>? sort = null, int? skip = null, int? limit = null);
    
    /// <summary>
    /// A method, used to get one entity from the database
    /// </summary>
    /// <param name="filter">A definition for the filters, which will be used to get the entity</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <returns><see cref="OperationResult"/>With result object containing the retrieved entities</returns>
    Task<OperationResult<TEntity>> GetOneAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken);
    
    /// <summary>
    /// A method, used to delete a single entity from the database
    /// </summary>
    /// <param name="filter">A definition for the filters, which will be used to get the entity</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <returns><see cref="OperationResult"/>With result object containing the deleted entity</returns>
    Task<OperationResult<TEntity>> DeleteOneAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken);
    
    /// <summary>
    /// A method, used for deleting many entities from the database
    /// </summary>
    /// <param name="filter">A definition for the filters, which will be used to get the entity</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken</param>
    /// <returns><see cref="OperationResult"/>With result object containing count of the deleted entities</returns>
    Task<OperationResult<long>> DeleteManyAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken);
    
    /// <summary>
    /// A method, used to check whether any entity exists matching the provided filter
    /// </summary>
    /// <param name="filter">A definition for the filters, which will be used to check existence</param>
    /// <param name="cancellationToken">An instance of <see cref="CancellationToken"/></param>
    /// <returns><see cref="OperationResult"/>With result object containing a boolean indicating whether any entity exists</returns>
    Task<OperationResult<bool>> AnyAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken);
}

