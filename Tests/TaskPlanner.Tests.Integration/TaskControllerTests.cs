using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using TaskPlanner.API.Core.Models.Task;
using TaskPlanner.API.Core.Services;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Data.Repositories;
using TaskPlanner.API.Web.Controllers;
using TaskPlanner.API.Web.Mapping;
using Task = System.Threading.Tasks.Task;
using TaskEntity = TaskPlanner.API.Data.Models.Task;
using TaskStatus = TaskPlanner.API.Data.Models.TaskStatus;

namespace TaskPlanner.Tests.Integration;

public class TaskControllerTests : IClassFixture<Mongo2GoFixture>
{
    private readonly Mongo2GoFixture _mongoFixture;

    public TaskControllerTests(Mongo2GoFixture mongoFixture)
    {
        _mongoFixture = mongoFixture;
    }

    private TaskController CreateAuthenticatedController(out ObjectId userId)
    {
        var taskRepository = new MongoDbRepositoryBase<TaskEntity>(_mongoFixture.Database, "Tasks");
        var categoryRepository = new MongoDbRepositoryBase<Category>(_mongoFixture.Database, "Categories");

        var service = new TaskService(taskRepository, categoryRepository);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
        });

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaskMappingProfile>();
        }, loggerFactory);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var controller = new TaskController(service, mapper);

        userId = ObjectId.GenerateNewId();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
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

    private TaskController CreateUnauthenticatedController()
    {
        var taskRepository = new MongoDbRepositoryBase<TaskEntity>(_mongoFixture.Database, "Tasks");
        var categoryRepository = new MongoDbRepositoryBase<Category>(_mongoFixture.Database, "Categories");

        var service = new TaskService(taskRepository, categoryRepository);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
        });
        
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaskMappingProfile>();
        }, loggerFactory);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var controller = new TaskController(service, mapper);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()) // no claims
            }
        };

        return controller;
    }

    private async Task<Category> InsertCategoryAsync(ObjectId userId, ObjectId categoryId, CancellationToken cancellationToken)
    {
        var categoryRepository = new MongoDbRepositoryBase<Category>(_mongoFixture.Database, "Categories");

        var category = new Category
        {
            Id = categoryId,
            UserId = userId,
            Name = "Work",
            Color = "#000000",
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow
        };

        await categoryRepository.CreateAsync(category);
        return category;
    }

    [Fact]
    public async Task Create_ShouldReturn_BadRequest_When_InputModel_Is_Null()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.Create(null, ObjectId.GenerateNewId().ToString(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn_BadRequest_When_Description_Is_NullOrWhitespace()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.Create(new CreateTaskInputModel { Description = " " }, ObjectId.GenerateNewId().ToString(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn_BadRequest_When_CategoryId_Is_NullOrWhitespace()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.Create(new CreateTaskInputModel { Description = "Task", Priority = TaskPriority.High }, "", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn_BadRequest_When_CategoryId_Is_Invalid()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.Create(new CreateTaskInputModel { Description = "Task", Priority = TaskPriority.High }, "not-an-object-id", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn_Unauthorized_When_User_Is_Not_Authenticated()
    {
        var controller = CreateUnauthenticatedController();

        var result = await controller.Create(new CreateTaskInputModel { Description = "Task", Priority = TaskPriority.High }, ObjectId.GenerateNewId().ToString(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn_BadRequest_When_Category_Does_Not_Exist_For_User()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var missingCategoryId = ObjectId.GenerateNewId();

        var result = await controller.Create(
            new CreateTaskInputModel { Description = "Task", Priority = TaskPriority.High },
            missingCategoryId.ToString(),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        // Also verify that nothing was created in DB
        var taskRepository = new MongoDbRepositoryBase<TaskEntity>(_mongoFixture.Database, "Tasks");
        var filter = Builders<TaskEntity>.Filter.Eq(x => x.UserId, userId);
        var get = await taskRepository.GetAsync(filter, CancellationToken.None);
        Assert.True(get.Success);
        Assert.Empty(get.ResultObject);
    }

    [Fact]
    public async Task Create_ShouldCreate_Task_InCategory_ForUser_And_DefaultStatus_Is_Todo()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var deadline = DateTime.UtcNow.AddDays(2);

        var result = await controller.Create(
            new CreateTaskInputModel
            {
                Description = "My task",
                Priority = TaskPriority.High,
                EstimatedMinutes = 45,
                Deadline = deadline
            },
            categoryId.ToString(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var response = Assert.IsType<TaskResponseModel>(ok.Value);
        Assert.Equal(userId.ToString(), response.UserId);
        Assert.Equal(categoryId.ToString(), response.CategoryId);
        Assert.Equal("My task", response.Description);
        Assert.Equal(TaskPriority.High, response.Priority);
        Assert.Equal(45, response.EstimatedMinutes);
        Assert.Equal(deadline, response.Deadline);

        // Verify persisted entity, including the business rule: default status is Todo on creation
        var taskRepository = new MongoDbRepositoryBase<TaskEntity>(_mongoFixture.Database, "Tasks");
        var filter = Builders<TaskEntity>.Filter.And(
            Builders<TaskEntity>.Filter.Eq(x => x.UserId, userId),
            Builders<TaskEntity>.Filter.Eq(x => x.CategoryId, categoryId)
        );

        var get = await taskRepository.GetAsync(filter, CancellationToken.None);
        Assert.True(get.Success);
        var tasks = get.ResultObject;
        Assert.Single(tasks);

        var created = tasks[0];
        Assert.Equal("My task", created.Description);
        Assert.Equal(TaskPriority.High, created.Priority);
        Assert.Equal(TaskStatus.Todo, created.Status);
        Assert.Equal(45, created.EstimatedMinutes);
        Assert.NotNull(created.Deadline);

        var diff = (created.Deadline.Value - deadline).Duration();
        Assert.True(diff < TimeSpan.FromMilliseconds(1), $"Deadline differs by {diff.TotalMilliseconds} ms");
        Assert.Equal(userId, created.UserId);
        Assert.Equal(categoryId, created.CategoryId);
        Assert.NotEqual(default, created.CreatedAt);
        Assert.NotNull(created.UpdatedAt);
    }

    [Fact]
    public async Task Create_ShouldNotAllow_Creating_Task_InCategory_OfAnotherUser()
    {
        var controllerA = CreateAuthenticatedController(out var userA);
        var controllerB = CreateAuthenticatedController(out var userB);

        var categoryIdOfA = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userA, categoryIdOfA, CancellationToken.None);

        var result = await controllerB.Create(
            new CreateTaskInputModel
            {
                Description = "Should fail",
                Priority = TaskPriority.Medium
            },
            categoryIdOfA.ToString(),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        // Verify no task was created for userB
        var taskRepository = new MongoDbRepositoryBase<TaskEntity>(_mongoFixture.Database, "Tasks");
        var filterB = Builders<TaskEntity>.Filter.Eq(x => x.UserId, userB);
        var getB = await taskRepository.GetAsync(filterB, CancellationToken.None);

        Assert.True(getB.Success);
        Assert.Empty(getB.ResultObject);

        // Verify no task was created under userA's category either
        var filterAinCat = Builders<TaskEntity>.Filter.And(
            Builders<TaskEntity>.Filter.Eq(x => x.UserId, userA),
            Builders<TaskEntity>.Filter.Eq(x => x.CategoryId, categoryIdOfA)
        );
        var getA = await taskRepository.GetAsync(filterAinCat, CancellationToken.None);

        Assert.True(getA.Success);
        Assert.Empty(getA.ResultObject);
    }
}
