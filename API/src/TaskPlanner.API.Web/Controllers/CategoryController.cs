using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Category;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Web.Extensions;

namespace TaskPlanner.API.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IValidator<CategoryInputModel> _categoryRequestValidator;
    private readonly IMapper _mapper;
    
    public CategoryController(ICategoryService categoryService, IValidator<CategoryInputModel> categoryRequestValidator, IMapper mapper)
    {
        _categoryService = categoryService;
        this._categoryRequestValidator = categoryRequestValidator;
        _mapper = mapper;
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

    [HttpPut]
    public IActionResult Put([FromBody] Category category)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpGet]
    public IActionResult Get([FromQuery] string entityId)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    [HttpDelete]
    public IActionResult Delete([FromQuery] string entityId)
    {
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}

