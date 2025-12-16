using System.Security.Claims;
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
    
    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryInputModel category, CancellationToken cancellationToken)
    {
        //Use built in validation methods
        if (category is null || string.IsNullOrWhiteSpace(category.Name)) return BadRequest();

        if (!this.TryGetUserObjectId(out var userId)) return Unauthorized();
        
        var create = await this._categoryService.CreateCategory(category, userId, cancellationToken);
        
        return Ok();
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

