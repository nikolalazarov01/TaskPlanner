using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Category;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;
using TaskPlanner.API.Web.Extensions;

namespace TaskPlanner.API.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IValidator<CategoryInputModel> _categoryRequestValidator;
    private readonly IValidator<UpdateCategoryInputModel> _updateCategoryRequestValidator;
    private readonly IMapper _mapper;
    
    public CategoryController(ICategoryService categoryService, IValidator<CategoryInputModel> categoryRequestValidator, IValidator<UpdateCategoryInputModel> updateCategoryRequestValidator, IMapper mapper)
    {
        _categoryService = categoryService;
        _categoryRequestValidator = categoryRequestValidator;
        _mapper = mapper;
        _updateCategoryRequestValidator = updateCategoryRequestValidator;
    }
    
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOne([FromRoute] string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        var deleteResult = await _categoryService.DeleteCategory(id, userId, cancellationToken);

        if (!deleteResult.Success)
        {
            if (deleteResult.Errors.Any(e => e is NotFoundError))
                return NotFound(deleteResult.Errors);

            return BadRequest(deleteResult.Errors);
        }

        var deleted = deleteResult.ResultObject;
        if (deleted is null) return NotFound();

        var response = _mapper.Map<CategoryResponseModel>(deleted);
        return Ok(response);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteMany(CancellationToken cancellationToken)
    {
        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();

        var deleteResult = await _categoryService.DeleteCategories(userId, cancellationToken);

        if (!deleteResult.Success)
        {
            if (deleteResult.Errors.Any(e => e is NotFoundError))
                return NotFound(deleteResult.Errors);

            return BadRequest(deleteResult.Errors);
        }

        // returns deleted count
        return Ok(new { deletedCount = deleteResult.ResultObject });
    }
}

