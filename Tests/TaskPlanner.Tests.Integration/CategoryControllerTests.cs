using System.Security.Claims;
using AutoMapper;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using TaskPlanner.API.Core.Models.Category;
using TaskPlanner.API.Core.Services;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Data.Repositories;
using TaskPlanner.API.Utilities.Constants;
using TaskPlanner.API.Web.Controllers;
using TaskPlanner.API.Web.Mapping;
using TaskPlanner.API.Web.Validation;
using Task = System.Threading.Tasks.Task;

namespace TaskPlanner.Tests.Integration;

public class CategoryControllerTests : IClassFixture<Mongo2GoFixture>
{
    //private readonly CategoryController _controller = new();
    private readonly Mongo2GoFixture _mongoFixture;

    public CategoryControllerTests(Mongo2GoFixture mongoFixture)
    {
        _mongoFixture = mongoFixture;
    }

    private CategoryController CreateAuthenticatedController()
    {
        var repository = new MongoDbRepositoryBase<Category>(_mongoFixture.Database, "Categories");
        var service = new CategoryService(repository);
        var validator = new CategoryValidator();
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
        });
        
        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<CategoryMappingProfile>(); },
            loggerFactory);

        mapperConfig.AssertConfigurationIsValid();

        IMapper mapper = mapperConfig.CreateMapper();
        
        var controller = new CategoryController(service, validator, mapper);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, ObjectId.GenerateNewId().ToString())
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };

        return controller;
    }


    [Fact]
    public void Post_ShouldReturn_BadRequest_When_InputModel_Is_Null()
    {
        var controller = CreateAuthenticatedController();

        var result = controller.Create(null, CancellationToken.None);

        var statusResult = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public void Post_ShouldReturn_BadRequest_When_Name_Is_Null()
    {
        var controller = CreateAuthenticatedController();
        var category = new CategoryInputModel { Name = null };

        var result = controller.Create(category, CancellationToken.None);

        var statusResult = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task Post_ShouldBe_Successful_When_Create()
    {
        var controller = CreateAuthenticatedController();
        var category = new CategoryInputModel { Name = "Work" };

        var result = await controller.Create(category, CancellationToken.None);

        var statusResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
        Assert.IsType<CategoryResponseModel>(statusResult.Value);
    }
    
    [Fact]
    public async Task Post_Should_Assign_DefaultValues_When_Not_Provided()
    {
        var controller = CreateAuthenticatedController();
        var category = new CategoryInputModel { Name = "Work" };

        var result = await controller.Create(category, CancellationToken.None);

        var statusResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
        
        var createdCategory = Assert.IsType<CategoryResponseModel>(statusResult.Value);

        Assert.Equal(ApiConstants.CategoryConstants.DefaultColor, createdCategory.Color);
        Assert.Equal(ApiConstants.CategoryConstants.DefaultSortOrder, createdCategory.SortOrder);
    }
    
    [Fact]
    public async Task Post_ShouldReturn_BadRequest_When_Color_Is_Invalid_Format()
    {
        var controller = CreateAuthenticatedController();

        var category = new CategoryInputModel
        {
            Name = "Work",
            Color = "red" // invalid, expected #FFF or #FFFFFF
        };

        var result = await controller.Create(category, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var errors = Assert.IsAssignableFrom<List<ValidationFailure>>(badRequest.Value);
        Assert.Contains(errors, e => e.PropertyName == "Color");
    }
    
    [Fact]
    public void Put_ShouldReturn_NotImplemented()
    {
        var controller = CreateAuthenticatedController();
        
        var category = new Category { Name = "Personal" };

        var result = controller.Put(category);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status501NotImplemented, statusResult.StatusCode);
    }

    [Fact]
    public void Get_ShouldReturn_NotImplemented()
    {
        var controller = CreateAuthenticatedController();
        
        var result = controller.Get("dummy-id");

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status501NotImplemented, statusResult.StatusCode);
    }

    [Fact]
    public void Delete_ShouldReturn_NotImplemented()
    {
        var controller = CreateAuthenticatedController();
        
        var result = controller.Delete("dummy-id");

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status501NotImplemented, statusResult.StatusCode);
    }
}

