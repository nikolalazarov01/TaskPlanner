using System.Security.Claims;
using AutoMapper;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
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
        var repository = new MongoDbRepositoryBase<Category>(_mongoFixture.Database);
        var tasksRepository = new MongoDbRepositoryBase<TaskPlanner.API.Data.Models.Task>(_mongoFixture.Database);
        
        var service = new CategoryService(repository);
        var taskService = new TaskService(tasksRepository, repository);
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
        
        var controller = new CategoryController(service, taskService, validator, updateValidator, mapper);

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
    
    [Fact]
    public async Task DeleteOne_ShouldReturn_BadRequest_When_Id_Is_NullOrWhitespace()
    {
        var controller = CreateAuthenticatedController();
    
        var result = await controller.DeleteOne(ObjectId.Empty, CancellationToken.None);
    
        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }
    
    [Fact]
    public async Task DeleteOne_ShouldReturn_NotFound_When_Category_Does_Not_Exist_For_User()
    {
        var controller = CreateAuthenticatedController();
    
        var result = await controller.DeleteOne(ObjectId.GenerateNewId(), CancellationToken.None);
    
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }
    
    [Fact]
    public async Task DeleteOne_ShouldReturn_Ok_With_CategoryResponseModel_When_Deleted()
    {
        var controller = CreateAuthenticatedController();
    
        // create first
        var createResult = await controller.Create(new CategoryInputModel { Name = "Work" }, CancellationToken.None);
        var created = Assert.IsType<OkObjectResult>(createResult).Value as CategoryResponseModel;
        Assert.NotNull(created);
    
        var deleteResult = await controller.DeleteOne(new ObjectId(created!.Id), CancellationToken.None);
    
        var ok = Assert.IsType<OkObjectResult>(deleteResult);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    
        var deleted = Assert.IsType<CategoryResponseModel>(ok.Value);
        Assert.Equal(created.Id, deleted.Id);
    
        // verify it is gone
        var getAfterDelete = await controller.GetOne(created.Id.ToString(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(getAfterDelete);
    }
    
    [Fact]
    public async Task DeleteOne_ShouldReturn_NotFound_When_Category_Belongs_To_Different_User()
    {
        var controllerA = CreateAuthenticatedController();
    
        // user A creates category
        var createResult = await controllerA.Create(new CategoryInputModel { Name = "Work" }, CancellationToken.None);
        var created = Assert.IsType<OkObjectResult>(createResult).Value as CategoryResponseModel;
        Assert.NotNull(created);
    
        // user B tries to delete it
        var controllerB = CreateAuthenticatedController();
    
        var deleteResult = await controllerB.DeleteOne(new ObjectId(created!.Id), CancellationToken.None);
    
        var notFound = Assert.IsType<NotFoundObjectResult>(deleteResult);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    
        // verify user A still can fetch it
        var getStillExists = await controllerA.GetOne(created.Id.ToString(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(getStillExists);
    }
    
    [Fact]
    public async Task DeleteManyByIds_ShouldReturn_BadRequest_When_CategoryIds_Is_Null()
    {
        var controller = CreateAuthenticatedController();
    
        var result = await controller.DeleteMany(null!, CancellationToken.None);
    
        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }
    
    [Fact]
    public async Task DeleteManyByIds_ShouldReturn_BadRequest_When_CategoryIds_Is_Empty()
    {
        var controller = CreateAuthenticatedController();
    
        var result = await controller.DeleteMany(Array.Empty<ObjectId>(), CancellationToken.None);
    
        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }
    
    [Fact]
    public async Task DeleteManyByIds_ShouldReturn_BadRequest_When_CategoryIds_Contains_Empty_ObjectId()
    {
        var controller = CreateAuthenticatedController();
    
        var result = await controller.DeleteMany(new[] { ObjectId.Empty }, CancellationToken.None);
    
        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }
    
    [Fact]
    public async Task DeleteManyByIds_ShouldReturn_Unauthorized_When_User_Is_Not_Authenticated()
    {
        var controller = CreateUnauthenticatedController();
    
        var result = await controller.DeleteMany(new[] { ObjectId.GenerateNewId() }, CancellationToken.None);
    
        var unauthorized = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }
    
    [Fact]
    public async Task DeleteManyByIds_ShouldReturn_NotFound_When_Some_Categories_Do_Not_Exist_For_User_And_Should_Not_Delete_Any()
    {
        var controller = CreateAuthenticatedController();
    
        // create 1 category for this user
        var create = await controller.Create(new CategoryInputModel { Name = "Work" }, CancellationToken.None);
        var created = Assert.IsType<OkObjectResult>(create).Value as CategoryResponseModel;
        Assert.NotNull(created);
    
        var missingId = ObjectId.GenerateNewId();
    
        var result = await controller.DeleteMany(new[] { new ObjectId(created!.Id), missingId }, CancellationToken.None);
    
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    
        // verify the existing category is still there
        var getAfter = await controller.GetOne(created.Id.ToString(), CancellationToken.None);
        Assert.IsType<OkObjectResult>(getAfter);
    }
    
    [Fact]
    public async Task DeleteManyByIds_ShouldReturn_NotFound_When_A_Category_Belongs_To_Different_User_And_Should_Not_Delete_Any()
    {
        var controllerA = CreateAuthenticatedController();
        var controllerB = CreateAuthenticatedController();
    
        var createA1 = await controllerA.Create(new CategoryInputModel { Name = "A1" }, CancellationToken.None);
        var createdA1 = Assert.IsType<OkObjectResult>(createA1).Value as CategoryResponseModel;
        Assert.NotNull(createdA1);
    
        var createB1 = await controllerB.Create(new CategoryInputModel { Name = "B1" }, CancellationToken.None);
        var createdB1 = Assert.IsType<OkObjectResult>(createB1).Value as CategoryResponseModel;
        Assert.NotNull(createdB1);
    
        // user A tries to delete A1 + B1 (B1 belongs to user B) => must fail and delete nothing
        var result = await controllerA.DeleteMany(
            new[] { new ObjectId(createdA1!.Id), new ObjectId(createdB1!.Id) },
            CancellationToken.None);
    
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    
        // both still exist
        Assert.IsType<OkObjectResult>(await controllerA.GetOne(createdA1.Id.ToString(), CancellationToken.None));
        Assert.IsType<OkObjectResult>(await controllerB.GetOne(createdB1.Id.ToString(), CancellationToken.None));
    }
    
    [Fact]
    public async Task DeleteManyByIds_ShouldReturn_Ok_With_DeletedCount_And_Delete_Corresponding_Tasks()
    {
        var controller = CreateAuthenticatedController();
        var userId = GetUserId(controller);
    
        // create 2 categories
        var c1Res = await controller.Create(new CategoryInputModel { Name = "C1" }, CancellationToken.None);
        var c1 = Assert.IsType<OkObjectResult>(c1Res).Value as CategoryResponseModel;
        Assert.NotNull(c1);
    
        var c2Res = await controller.Create(new CategoryInputModel { Name = "C2" }, CancellationToken.None);
        var c2 = Assert.IsType<OkObjectResult>(c2Res).Value as CategoryResponseModel;
        Assert.NotNull(c2);
    
        var c1Id = new ObjectId(c1!.Id);
        var c2Id = new ObjectId(c2!.Id);
    
        // seed tasks in both categories
        await InsertTaskAsync(userId, c1Id, cancellationToken: CancellationToken.None);
        await InsertTaskAsync(userId, c1Id, cancellationToken: CancellationToken.None);
        await InsertTaskAsync(userId, c2Id, cancellationToken: CancellationToken.None);
    
        // sanity: tasks exist
        var tasksRepo = new MongoDbRepositoryBase<TaskPlanner.API.Data.Models.Task>(_mongoFixture.Database, "Tasks");
        var beforeTasks = await tasksRepo.GetAsync(
            Builders<TaskPlanner.API.Data.Models.Task>.Filter.Eq(x => x.UserId, userId),
            CancellationToken.None);
    
        Assert.True(beforeTasks.Success);
        Assert.Equal(3, beforeTasks.ResultObject?.Count ?? 0);
    
        var result = await controller.DeleteMany(new[] { c1Id, c2Id }, CancellationToken.None);
    
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    
        var deletedCount = Assert.IsType<long>(ok.Value);
        Assert.Equal(2L, deletedCount);
    
        // categories gone
        Assert.IsType<NotFoundResult>(await controller.GetOne(c1Id.ToString(), CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.GetOne(c2Id.ToString(), CancellationToken.None));
    
        // tasks for those categories are deleted (cascade)
        var afterTasks = await tasksRepo.GetAsync(
            Builders<TaskPlanner.API.Data.Models.Task>.Filter.Eq(x => x.UserId, userId),
            CancellationToken.None);
    
        Assert.True(afterTasks.Success);
        Assert.Empty(afterTasks.ResultObject);
    }

    
    [Fact]
    public async Task DeleteByUserId_ShouldReturn_NotFound_When_User_Has_No_Categories()
    {
        var controller = CreateAuthenticatedController();
    
        var result = await controller.DeleteByUserId(CancellationToken.None);
    
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }
    
    [Fact]
    public async Task DeleteByUserId_ShouldReturn_Ok_With_DeletedCount_When_User_Has_Categories()
    {
        var controller = CreateAuthenticatedController();
    
        await controller.Create(new CategoryInputModel { Name = "Work" }, CancellationToken.None);
        await controller.Create(new CategoryInputModel { Name = "Personal" }, CancellationToken.None);
    
        var deleteManyResult = await controller.DeleteByUserId(CancellationToken.None);
    
        var ok = Assert.IsType<OkObjectResult>(deleteManyResult);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    
        // anonymous object { deletedCount = N }
        var value = ok.Value;
        Assert.NotNull(value);
        var deletedCountProp = value.GetType().GetProperty("deletedCount");
        Assert.NotNull(deletedCountProp);
    
        var deletedCount = (long)deletedCountProp!.GetValue(value)!;
        Assert.Equal(2L, deletedCount);
    
        // verify list is now empty
        var getManyAfterDelete = await controller.GetMany(CancellationToken.None);
        var okAfter = Assert.IsType<OkObjectResult>(getManyAfterDelete);
        var list = Assert.IsAssignableFrom<List<CategoryResponseModel>>(okAfter.Value);
        Assert.Empty(list);
    }
    
    [Fact]
    public async Task DeleteByUserId_ShouldOnly_Delete_Current_User_Categories()
    {
        var controllerA = CreateAuthenticatedController();
        var controllerB = CreateAuthenticatedController();
    
        await controllerA.Create(new CategoryInputModel { Name = "A1" }, CancellationToken.None);
        await controllerA.Create(new CategoryInputModel { Name = "A2" }, CancellationToken.None);
        await controllerB.Create(new CategoryInputModel { Name = "B1" }, CancellationToken.None);
    
        // delete only A's categories
        var deleteA = await controllerA.DeleteByUserId(CancellationToken.None);
        Assert.IsType<OkObjectResult>(deleteA);
    
        // A now has none
        var getA = await controllerA.GetMany(CancellationToken.None);
        var okA = Assert.IsType<OkObjectResult>(getA);
        var listA = Assert.IsAssignableFrom<List<CategoryResponseModel>>(okA.Value);
        Assert.Empty(listA);
    
        // B still has theirs
        var getB = await controllerB.GetMany(CancellationToken.None);
        var okB = Assert.IsType<OkObjectResult>(getB);
        var listB = Assert.IsAssignableFrom<List<CategoryResponseModel>>(okB.Value);
        Assert.Single(listB);
        Assert.Equal("B1", listB[0].Name);
    }
    
    private static ObjectId GetUserId(CategoryController controller)
    {
        var id = controller.ControllerContext.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ObjectId.Parse(id!);
    }
    
    private CategoryController CreateUnauthenticatedController()
    {
        var repository = new MongoDbRepositoryBase<Category>(_mongoFixture.Database, "Categories");
        var tasksRepository = new MongoDbRepositoryBase<TaskPlanner.API.Data.Models.Task>(_mongoFixture.Database, "Tasks");
    
        var service = new CategoryService(repository);
        var taskService = new TaskService(tasksRepository, repository);
        var validator = new CategoryValidator();
        var updateValidator = new UpdateCategoryValidator();
    
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
        });
    
        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<CategoryMappingProfile>(); }, loggerFactory);
        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();
    
        var controller = new CategoryController(service, taskService, validator, updateValidator, mapper);
    
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()) // no claims
            }
        };
    
        return controller;
    }
    
    private async Task<TaskPlanner.API.Data.Models.Task> InsertTaskAsync(ObjectId userId, ObjectId categoryId, ObjectId? taskId = null, CancellationToken cancellationToken = default)
    {
        var taskRepository = new MongoDbRepositoryBase<TaskPlanner.API.Data.Models.Task>(_mongoFixture.Database, "Tasks");
    
        var entity = new TaskPlanner.API.Data.Models.Task
        {
            Id = taskId ?? ObjectId.GenerateNewId(),
            UserId = userId,
            CategoryId = categoryId,
            Description = "Seed task",
            Priority = TaskPlanner.API.Data.Models.TaskPriority.Medium,
            Status = TaskPlanner.API.Data.Models.TaskStatus.Todo,
            EstimatedMinutes = 10,
            Deadline = DateTime.UtcNow.AddDays(2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    
        await taskRepository.CreateAsync(entity);
        return entity;
    }
}

