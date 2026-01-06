using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Category;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;
using TaskPlanner.API.Web.Extensions;

namespace TaskPlanner.API.Web.Controllers;

/// <summary>
/// Exposes endpoints for managing categories for the authenticated user
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ITaskService _taskService;
    private readonly IValidator<CategoryInputModel> _categoryRequestValidator;
    private readonly IValidator<UpdateCategoryInputModel> _updateCategoryRequestValidator;
    private readonly ITransactionManagementUtility _transactionManagementUtility;
    private readonly IMapper _mapper;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryController"/>
    /// </summary>
    /// <param name="categoryService">Service that contains the business logic for categories</param>
    /// <param name="taskService">Service that contains the business logic for tasks</param>
    /// <param name="categoryRequestValidator">Validator for <see cref="CategoryInputModel"/></param>
    /// <param name="updateCategoryRequestValidator">Validator for <see cref="UpdateCategoryInputModel"/></param>
    /// <param name="mapper">Mapper used to map entities to response models</param>
    public CategoryController(ICategoryService categoryService, ITaskService taskService, IValidator<CategoryInputModel> categoryRequestValidator, IValidator<UpdateCategoryInputModel> updateCategoryRequestValidator, IMapper mapper, ITransactionManagementUtility transactionManagementUtility)
    {
        _categoryService = categoryService;
        _taskService = taskService;
        _categoryRequestValidator = categoryRequestValidator;
        _mapper = mapper;
        _transactionManagementUtility = transactionManagementUtility;
        _updateCategoryRequestValidator = updateCategoryRequestValidator;
    }
    
    /// <summary>
    /// Create - Category
    /// </summary>
    /// <param name="category">The input model used to create a new category</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled</param>
    /// <returns>Returns the created category as <see cref="CategoryResponseModel"/> if the request is successful</returns>
    /// <remarks>
    /// Creates a category for the authenticated user. The authenticated user id is extracted from the request context.
    /// </remarks>
    /// <response code="200">Returns the created category</response>
    /// <response code="400">The input is invalid or an error occurred during creation</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">The created entity could not be returned</response>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryInputModel category, CancellationToken cancellationToken)
    {
        if (category is null || string.IsNullOrWhiteSpace(category.Name)) return BadRequest();

        var validation = await _categoryRequestValidator.ValidateAsync(category, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.Errors);
        
        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();
        
        var create = await this._categoryService.CreateCategory(category, userId, cancellationToken);
        
        if (!create.Success) return BadRequest();
        var createdEntity = create.ResultObject;
        if (createdEntity is null) return NotFound();
        
        var response = _mapper.Map<CategoryResponseModel>(createdEntity);
        return Ok(response);
    }

    /// <summary>
    /// Update - Category
    /// </summary>
    /// <param name="category">The input model containing the category id and the fields to update</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled</param>
    /// <returns>Returns the updated category as <see cref="CategoryResponseModel"/> if the request is successful</returns>
    /// <remarks>
    /// Updates a category belonging to the authenticated user. Only provided fields are updated.
    /// </remarks>
    /// <response code="200">Returns the updated category</response>
    /// <response code="400">The input is invalid or an error occurred during update</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">The category could not be found</response>
    [HttpPatch]
    public async Task<IActionResult> Update([FromBody] UpdateCategoryInputModel category, CancellationToken cancellationToken)
    {
        if (category is null || string.IsNullOrWhiteSpace(category.Id)) return BadRequest();

        var validation = await this._updateCategoryRequestValidator.ValidateAsync(category, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.Errors);

        if (!this.TryGetUserObjectId(out var userId))
            return Unauthorized();

        var updateResult = await _categoryService.UpdateCategory(category, userId, cancellationToken);

        if (!updateResult.Success) return BadRequest(updateResult.Errors);
        var updatedEntity = updateResult.ResultObject;
        if (updatedEntity is null) return NotFound();

        var response = _mapper.Map<CategoryResponseModel>(updatedEntity);
        return Ok(response);
    }

    /// <summary>
    /// Get - Category
    /// </summary>
    /// <param name="id">The identifier of the category</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled</param>
    /// <returns>Returns a single category as <see cref="CategoryResponseModel"/> if found</returns>
    /// <remarks>
    /// Retrieves a single category by id for the authenticated user.
    /// </remarks>
    /// <response code="200">Returns the category</response>
    /// <response code="400">The request is invalid or an error occurred during retrieval</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">The category was not found</response>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOne([FromRoute] string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        var getResult = await _categoryService.GetCategoryById(id, userId, cancellationToken);

        if (!getResult.Success)
        {
            if (getResult.Errors.Any(e => e is NotFoundError)) return NotFound();
            return BadRequest(getResult.Errors);
        }

        var entity = getResult.ResultObject;
        if (entity is null) return NotFound();

        var response = _mapper.Map<CategoryResponseModel>(entity);
        return Ok(response);
    }

    /// <summary>
    /// Get - Categories
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled</param>
    /// <returns>Returns a list of categories as <see cref="CategoryResponseModel"/> for the authenticated user</returns>
    /// <remarks>
    /// Retrieves all categories belonging to the authenticated user.
    /// </remarks>
    /// <response code="200">Returns the list of categories (can be empty)</response>
    /// <response code="400">The request is invalid or an error occurred during retrieval</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">No categories were found (depending on service/repository behavior)</response>
    [HttpGet]
    public async Task<IActionResult> GetMany(CancellationToken cancellationToken)
    {
        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        var getResult = await _categoryService.GetCategories(userId, cancellationToken);

        if (!getResult.Success)
        {
            if (getResult.Errors.Any(e => e is NotFoundError)) return NotFound(getResult.Errors);
            return BadRequest(getResult.Errors);
        }

        var entities = getResult.ResultObject ?? Array.Empty<Category>();

        var response = _mapper.Map<List<CategoryResponseModel>>(entities);
        return Ok(response);
    }

    /// <summary>
    /// Delete - Category
    /// </summary>
    /// <param name="id">The identifier of the category to delete</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled</param>
    /// <returns>Returns the deleted category as <see cref="CategoryResponseModel"/> if the request is successful</returns>
    /// <remarks>
    /// Deletes a single category by id for the authenticated user.
    /// </remarks>
    /// <response code="200">Returns the deleted category</response>
    /// <response code="400">The request is invalid or an error occurred during deletion</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">The category was not found</response>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOne([FromRoute] ObjectId id, CancellationToken cancellationToken)
    {
        if (id == ObjectId.Empty) return BadRequest();

        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        return await this.DeleteCategoryInternally(id, userId, cancellationToken);
    }

    /// <summary>
    /// Delete - Categories (selected)
    /// </summary>
    /// <param name="categoryIds">The identifiers of the categories to delete.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled</param>
    /// <returns>
    /// Returns the number of deleted categories if the request is successful.
    /// </returns>
    /// <remarks>
    /// Deletes multiple categories for the authenticated user in a single operation.
    /// The authenticated user id is extracted from the request context.
    /// All provided category ids must exist and belong to the authenticated user; otherwise no deletions are applied.
    /// On successful category deletion, all tasks belonging to the deleted categories are also deleted.
    /// </remarks>
    /// <response code="200">Returns the number of deleted categories</response>
    /// <response code="400">The input is invalid or an error occurred during deletion</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">One or more categories were not found</response>
    [HttpDelete]
    public async Task<IActionResult> DeleteMany([FromQuery] ObjectId[] categoryIds, CancellationToken cancellationToken)
    {
        if (categoryIds is null || categoryIds.Length == 0) return BadRequest();
        if (categoryIds.Any(x => x == ObjectId.Empty)) return BadRequest();
    
        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();
    
        return await this.DeleteManyCategoriesInternally(categoryIds, userId, cancellationToken);
    }
    
    /// <summary>
    /// Delete - Categories
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be cancelled</param>
    /// <returns>Returns the number of deleted categories for the authenticated user</returns>
    /// <remarks>
    /// Deletes all categories belonging to the authenticated user.
    /// </remarks>
    /// <response code="200">Returns the count of deleted categories</response>
    /// <response code="400">The request is invalid or an error occurred during deletion</response>
    /// <response code="401">The request is unauthorized</response>
    /// <response code="404">No categories were found to delete (depending on service/repository behavior)</response>
    [HttpDelete("by-user-id")]
    public async Task<IActionResult> DeleteByUserId(CancellationToken cancellationToken)
    {
        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        return await this.DeleteCategoriesByUserIdInternally(userId, cancellationToken);
    }

    private async Task<IActionResult> DeleteCategoryInternally(ObjectId id, ObjectId userId, CancellationToken cancellationToken)
    {
        var deleteCategory = await this._transactionManagementUtility.ExecuteInTransactionAsync(async () =>
        {
            var operationResult = new OperationResult<Category>();

            var deleteCorrespondingTasks = await this._taskService.DeleteByCategoryId(id, userId, cancellationToken);

            if (!deleteCorrespondingTasks.Success) return operationResult.AppendErrors(deleteCorrespondingTasks);
            
            var deleteResult = await _categoryService.DeleteCategory(id, userId, cancellationToken);
            if (!deleteResult.Success) return operationResult.AppendErrors(deleteResult);

            if (deleteResult.ResultObject is null) return operationResult.AppendError("Deletion encountered errors");

            var deleted = deleteResult.ResultObject;
            if (deleted is null) return operationResult.AppendError("Something went wrong");


            return operationResult.WithRelatedObject(deleteResult.ResultObject);
        }, cancellationToken);
        
        if (!deleteCategory.Success)
        {
            if (deleteCategory.Errors.Any(e => e is NotFoundError))
                return NotFound(deleteCategory.Errors);
            
            return BadRequest(deleteCategory.Errors);
        }

        var result = _mapper.Map<CategoryResponseModel>(deleteCategory.ResultObject);
        return Ok(result);
    }

    private async Task<IActionResult> DeleteManyCategoriesInternally(ObjectId[] categoryIds, ObjectId userId, CancellationToken cancellationToken)
    {
        var deleteCategories = await this._transactionManagementUtility.ExecuteInTransactionAsync(async () =>
        {
            var operationResult = new OperationResult<long>();

            var deleteCorrespondingTasks =
                await _taskService.DeleteByCategoryId(categoryIds, userId, cancellationToken);

            if (!deleteCorrespondingTasks.Success) return operationResult.AppendErrors(deleteCorrespondingTasks);

            var deleteResult = await _categoryService.DeleteCategories(categoryIds, userId, cancellationToken);

            if (!deleteResult.Success) return operationResult.AppendErrors(deleteResult);

            return operationResult.WithRelatedObject(deleteResult.ResultObject);
        }, cancellationToken);
        
        if (!deleteCategories.Success)
        {
            if (deleteCategories.Errors.Any(e => e is NotFoundError))
                return NotFound(deleteCategories.Errors);
            
            return BadRequest(deleteCategories.Errors);
        }

        return Ok(deleteCategories.ResultObject);
    }

    private async Task<IActionResult> DeleteCategoriesByUserIdInternally(ObjectId userId, CancellationToken cancellationToken)
    {
        var deleteCategories = await this._transactionManagementUtility.ExecuteInTransactionAsync(async () =>
        {
            var operationResult = new OperationResult<long>();

            var deleteResult = await _categoryService.DeleteCategories(userId, cancellationToken);

            if (!deleteResult.Success) return operationResult.AppendErrors(deleteResult);

            var deleteCorrespondingTasks = await this._taskService.DeleteMany(userId, cancellationToken);

            if (!deleteCorrespondingTasks.Success) return operationResult.AppendErrors(deleteCorrespondingTasks);

            return operationResult.WithRelatedObject(deleteResult.ResultObject);
        }, cancellationToken);
            
        if (!deleteCategories.Success)
        {
            if (deleteCategories.Errors.Any(e => e is NotFoundError))
                return NotFound(deleteCategories.Errors);
            
            return BadRequest(deleteCategories.Errors);
        }
            
        return Ok(new { deletedCount = deleteCategories.ResultObject });
    }
}
