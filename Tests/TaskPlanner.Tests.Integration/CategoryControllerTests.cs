using System.Security.Claims;
using AutoMapper;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
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
        var updateValidator = new UpdateCategoryValidator();
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
        });
        
        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<CategoryMappingProfile>(); },
            loggerFactory);

        mapperConfig.AssertConfigurationIsValid();

        IMapper mapper = mapperConfig.CreateMapper();
        
        var controller = new CategoryController(service, validator, updateValidator, mapper);

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
    public async Task Post_ShouldReturn_BadRequest_When_InputModel_Is_Null()
    {
        var controller = CreateAuthenticatedController();

        var result = await controller.Create(null, CancellationToken.None);

        var statusResult = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task Post_ShouldReturn_BadRequest_When_Name_Is_Null()
    {
        var controller = CreateAuthenticatedController();
        var category = new CategoryInputModel { Name = null };

        var result = await controller.Create(category, CancellationToken.None);

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
    public async Task Patch_ShouldReturn_BadRequest_When_InputModel_Is_Null()
    {
        var controller = CreateAuthenticatedController();

        var result = await controller.Update(null, CancellationToken.None);

        var statusResult = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task Patch_ShouldReturn_BadRequest_When_Id_Is_NullOrEmpty()
    {
        var controller = CreateAuthenticatedController();

        var result = await controller.Update(new UpdateCategoryInputModel { Id = "" }, CancellationToken.None);

        var statusResult = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusResult.StatusCode);
    }

    [Fact]
    public async Task Patch_ShouldReturn_BadRequest_When_Color_Is_Invalid_Format()
    {
        var controller = CreateAuthenticatedController();

        // create first
        var createResult = await controller.Create(new CategoryInputModel { Name = "Work" }, CancellationToken.None);
        var created = Assert.IsType<OkObjectResult>(createResult).Value as CategoryResponseModel;
        Assert.NotNull(created);

        // update with invalid hex
        var updateModel = new UpdateCategoryInputModel
        {
            Id = created!.Id.ToString(),
            Color = "red"
        };

        var result = await controller.Update(updateModel, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var errors = Assert.IsAssignableFrom<List<ValidationFailure>>(badRequest.Value);
        Assert.Contains(errors, e => e.PropertyName == "Color");
    }

    [Fact]
    public async Task Patch_ShouldBe_Successful_And_Return_ResponseModel_When_Updating_Name_And_Color()
    {
        var controller = CreateAuthenticatedController();

        // create first
        var createResult = await controller.Create(new CategoryInputModel { Name = "Work" }, CancellationToken.None);
        var created = Assert.IsType<OkObjectResult>(createResult).Value as CategoryResponseModel;
        Assert.NotNull(created);

        var newName = "Work Updated";
        var newColor = "#FFFFFF";

        var updateModel = new UpdateCategoryInputModel
        {
            Id = created!.Id.ToString(),
            Name = newName,
            Color = newColor
        };

        var result = await controller.Update(updateModel, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var updated = Assert.IsType<CategoryResponseModel>(ok.Value);
        Assert.Equal(newName, updated.Name);
        Assert.Equal(newColor, updated.Color);
    }

    [Fact]
    public async Task Patch_Should_Keep_Old_Values_When_Optional_Fields_Are_Null()
    {
        var controller = CreateAuthenticatedController();

        // create with explicit values so we can verify they remain
        var createInput = new CategoryInputModel
        {
            Name = "Work",
            Color = "#ABCDEF",
            SortOrder = 5
        };

        var createResult = await controller.Create(createInput, CancellationToken.None);
        var created = Assert.IsType<OkObjectResult>(createResult).Value as CategoryResponseModel;
        Assert.NotNull(created);

        // update only name (Color and SortOrder null => should be kept)
        var updateModel = new UpdateCategoryInputModel
        {
            Id = created!.Id.ToString(),
            Name = "Work Renamed",
            Color = null,
            SortOrder = null
        };

        var result = await controller.Update(updateModel, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<CategoryResponseModel>(ok.Value);

        Assert.Equal("Work Renamed", updated.Name);
        Assert.Equal("#ABCDEF", updated.Color);
        Assert.Equal(5, updated.SortOrder);
    }

    [Fact]
    public async Task Patch_ShouldReturn_BadRequest_When_Category_Not_Found()
    {
        var controller = CreateAuthenticatedController();

        var updateModel = new UpdateCategoryInputModel
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = "Does not matter"
        };

        var result = await controller.Update(updateModel, CancellationToken.None);

        // Service/repo returns not found as an error => controller returns BadRequest(errors)
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }
    
    [Fact]
    public async Task GetOne_ShouldReturn_BadRequest_When_Id_Is_NullOrWhitespace()
    {
        var controller = CreateAuthenticatedController();
    
        var result = await controller.GetOne("", CancellationToken.None);
    
        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }
    
    [Fact]
    public async Task GetOne_ShouldReturn_NotFound_When_Category_Does_Not_Exist_For_User()
    {
        var controller = CreateAuthenticatedController();
    
        var result = await controller.GetOne(ObjectId.GenerateNewId().ToString(), CancellationToken.None);
    
        var notFound = Assert.IsType<NotFoundResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }
    
    [Fact]
    public async Task GetOne_ShouldReturn_Ok_With_CategoryResponseModel_When_Found()
    {
        var controller = CreateAuthenticatedController();
    
        // create first
        var createResult = await controller.Create(new CategoryInputModel { Name = "Work" }, CancellationToken.None);
        var created = Assert.IsType<OkObjectResult>(createResult).Value as CategoryResponseModel;
        Assert.NotNull(created);
    
        var result = await controller.GetOne(created!.Id.ToString(), CancellationToken.None);
    
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    
        var response = Assert.IsType<CategoryResponseModel>(ok.Value);
        Assert.Equal(created.Id, response.Id);
        Assert.Equal(created.Name, response.Name);
        Assert.Equal(created.Color, response.Color);
        Assert.Equal(created.SortOrder, response.SortOrder);
    }
    
    [Fact]
    public async Task GetMany_ShouldReturn_Ok_With_Empty_List_When_No_Categories()
    {
        var controller = CreateAuthenticatedController();
    
        var result = await controller.GetMany(CancellationToken.None);
    
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    
        var list = Assert.IsAssignableFrom<List<CategoryResponseModel>>(ok.Value);
        Assert.Empty(list);
    }
    
    [Fact]
    public async Task GetMany_ShouldReturn_Ok_With_List_When_Categories_Exist()
    {
        var controller = CreateAuthenticatedController();
    
        await controller.Create(new CategoryInputModel { Name = "Work" }, CancellationToken.None);
        await controller.Create(new CategoryInputModel { Name = "Personal" }, CancellationToken.None);
    
        var result = await controller.GetMany(CancellationToken.None);
    
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    
        var list = Assert.IsAssignableFrom<List<CategoryResponseModel>>(ok.Value);
        Assert.Equal(2, list.Count);
        Assert.Contains(list, x => x.Name == "Work");
        Assert.Contains(list, x => x.Name == "Personal");
    }
    
    [Fact]
    public async Task GetOne_ShouldReturn_NotFound_When_Category_Belongs_To_Different_User()
    {
        // Controller with user A creates category
        var controllerA = CreateAuthenticatedController();
    
        var createResult = await controllerA.Create(new CategoryInputModel { Name = "Work" }, CancellationToken.None);
        var created = Assert.IsType<OkObjectResult>(createResult).Value as CategoryResponseModel;
        Assert.NotNull(created);
    
        // Controller with user B tries to fetch same category
        var controllerB = CreateAuthenticatedController();
    
        var result = await controllerB.GetOne(created!.Id.ToString(), CancellationToken.None);
    
        var notFound = Assert.IsType<NotFoundResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }
}

