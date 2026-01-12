using System.Security.Claims;
using AutoMapper;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using TaskPlanner.API.Core.Models.Task;
using TaskPlanner.API.Core.Services;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Web.Controllers;
using TaskPlanner.API.Web.Mapping;
using TaskPlanner.API.Web.Validation;
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
        var taskRepository = TestPreparationData.CreateRepository<TaskPlanner.API.Data.Models.Task>(this._mongoFixture);
        var categoryRepository = TestPreparationData.CreateRepository<Category>(this._mongoFixture);
        var taskExecutionLogRepository = TestPreparationData.CreateRepository<TaskExecutionLog>(this._mongoFixture);

        var taskService = new TaskService(taskRepository, categoryRepository);
        var taskLogService = new TaskLogService(taskExecutionLogRepository, taskRepository);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
        });

        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<TaskMappingProfile>(); }, loggerFactory);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var createValidator = new TaskValidator();
        var updateValidator = new UpdateTaskValidator();

        var controller = new TaskController(taskService, taskLogService, mapper, createValidator, updateValidator);

        userId = ObjectId.GenerateNewId();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };

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
        var taskRepository = TestPreparationData.CreateRepository<TaskPlanner.API.Data.Models.Task>(this._mongoFixture);
        var categoryRepository = TestPreparationData.CreateRepository<Category>(this._mongoFixture);
        var taskExecutionLogRepository = TestPreparationData.CreateRepository<TaskExecutionLog>(this._mongoFixture);

        var taskService = new TaskService(taskRepository, categoryRepository);
        var taskLogService = new TaskLogService(taskExecutionLogRepository, taskRepository);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
        });

        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<TaskMappingProfile>(); }, loggerFactory);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var createValidator = new TaskValidator();
        var updateValidator = new UpdateTaskValidator();

        var controller = new TaskController(taskService, taskLogService, mapper, createValidator, updateValidator);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()) // no claims
            }
        };

        return controller;
    }

    private async Task<Category> InsertCategoryAsync(ObjectId userId, ObjectId categoryId,
        CancellationToken cancellationToken)
    {
        var categoryRepository = TestPreparationData.CreateRepository<Category>(this._mongoFixture);

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

    private async Task<TaskEntity> InsertTaskAsync(ObjectId userId, ObjectId categoryId, ObjectId taskId,
        CancellationToken cancellationToken)
    {
        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);

        var entity = new TaskEntity
        {
            Id = taskId,
            UserId = userId,
            CategoryId = categoryId,
            Description = "Seed task",
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Todo,
            EstimatedMinutes = 10,
            Deadline = DateTime.UtcNow.AddDays(3),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await taskRepository.CreateAsync(entity);
        return entity;
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
    public async Task Create_ShouldReturn_BadRequestObject_When_Description_Is_NullOrWhitespace()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.Create(new CreateTaskInputModel { Description = " " },
            ObjectId.GenerateNewId().ToString(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var errors = Assert.IsAssignableFrom<List<ValidationFailure>>(badRequest.Value);
        Assert.Contains(errors, e => e.PropertyName == nameof(CreateTaskInputModel.Description));
    }

    [Fact]
    public async Task Create_ShouldReturn_BadRequest_When_CategoryId_Is_NullOrWhitespace()
    {
        var controller = CreateAuthenticatedController(out _);

        var result =
            await controller.Create(new CreateTaskInputModel { Description = "Task", Priority = TaskPriority.High }, "",
                CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn_BadRequest_When_CategoryId_Is_Invalid()
    {
        var controller = CreateAuthenticatedController(out _);

        var result =
            await controller.Create(new CreateTaskInputModel { Description = "Task", Priority = TaskPriority.High },
                "not-an-object-id", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn_Unauthorized_When_User_Is_Not_Authenticated()
    {
        var controller = CreateUnauthenticatedController();

        var result =
            await controller.Create(new CreateTaskInputModel { Description = "Task", Priority = TaskPriority.High },
                ObjectId.GenerateNewId().ToString(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturn_BadRequest_When_Description_Exceeds_50_Characters()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var longDescription = new string('a', 51);

        var result = await controller.Create(
            new CreateTaskInputModel { Description = longDescription, Priority = TaskPriority.Medium },
            categoryId.ToString(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var errors = Assert.IsAssignableFrom<List<ValidationFailure>>(badRequest.Value);
        Assert.Contains(errors, e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Create_ShouldReturn_BadRequest_When_Deadline_Is_In_The_Past()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var pastDeadline = DateTime.UtcNow.AddMinutes(-1);

        var result = await controller.Create(
            new CreateTaskInputModel { Description = "Valid", Priority = TaskPriority.Medium, Deadline = pastDeadline },
            categoryId.ToString(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var errors = Assert.IsAssignableFrom<List<ValidationFailure>>(badRequest.Value);
        Assert.Contains(errors, e => e.PropertyName == "Deadline");
    }

    [Fact]
    public async Task Create_ShouldReturn_BadRequest_When_EstimatedMinutes_Is_Negative()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var result = await controller.Create(
            new CreateTaskInputModel { Description = "Valid", Priority = TaskPriority.Medium, EstimatedMinutes = -1 },
            categoryId.ToString(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var errors = Assert.IsAssignableFrom<List<ValidationFailure>>(badRequest.Value);
        Assert.Contains(errors, e => e.PropertyName == "EstimatedMinutes");
    }

    [Fact]
    public async Task Create_ShouldReturn_BadRequest_When_Category_Does_Not_Exist_For_User()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var missingCategoryId = ObjectId.GenerateNewId();

        var result = await controller.Create(
            new CreateTaskInputModel { Description = "Task", Priority = TaskPriority.High },
            missingCategoryId.ToString(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
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
                Description = "My task", Priority = TaskPriority.High, EstimatedMinutes = 45, Deadline = deadline
            }, categoryId.ToString(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var response = Assert.IsType<TaskResponseModel>(ok.Value);
        Assert.Equal(userId.ToString(), response.UserId);
        Assert.Equal(categoryId.ToString(), response.CategoryId);
        Assert.Equal("My task", response.Description);
        Assert.Equal(TaskPriority.High, response.Priority);
        Assert.Equal(45, response.EstimatedMinutes);

        Assert.NotNull(response.Deadline);
        var diffResp = (response.Deadline.Value - deadline).Duration();
        Assert.True(diffResp < TimeSpan.FromMilliseconds(1), $"Deadline differs by {diffResp.TotalMilliseconds} ms");

        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var filter = Builders<TaskEntity>.Filter.And(Builders<TaskEntity>.Filter.Eq(x => x.UserId, userId),
            Builders<TaskEntity>.Filter.Eq(x => x.CategoryId, categoryId));

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
        var diffDb = (created.Deadline.Value - deadline).Duration();
        Assert.True(diffDb < TimeSpan.FromMilliseconds(1), $"Deadline differs by {diffDb.TotalMilliseconds} ms");

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
            new CreateTaskInputModel { Description = "Should fail", Priority = TaskPriority.Medium },
            categoryIdOfA.ToString(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);

        var filterB = Builders<TaskEntity>.Filter.Eq(x => x.UserId, userB);
        var getB = await taskRepository.GetAsync(filterB, CancellationToken.None);
        Assert.True(getB.Success);
        Assert.Empty(getB.ResultObject);

        var filterAinCat = Builders<TaskEntity>.Filter.And(Builders<TaskEntity>.Filter.Eq(x => x.UserId, userA),
            Builders<TaskEntity>.Filter.Eq(x => x.CategoryId, categoryIdOfA));
        var getA = await taskRepository.GetAsync(filterAinCat, CancellationToken.None);
        Assert.True(getA.Success);
        Assert.Empty(getA.ResultObject);
    }

    [Fact]
    public async Task Update_ShouldReturn_BadRequest_When_Description_Exceeds_50_Characters()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        await InsertTaskAsync(userId, categoryId, taskId, CancellationToken.None);

        var longDescription = new string('a', 51);

        var result = await controller.Update(
            new UpdateTaskInputModel
            {
                Id = taskId.ToString(),
                CategoryId = categoryId.ToString(),
                Description = longDescription,
                Deadline = DateTime.UtcNow.AddDays(1),
                Priority = TaskPriority.Medium,
                Status = TaskStatus.Todo,
                EstimatedMinutes = 10
            }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var errors = Assert.IsAssignableFrom<List<ValidationFailure>>(badRequest.Value);
        Assert.Contains(errors, e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task Update_ShouldReturn_BadRequest_When_Deadline_Is_In_The_Past()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        await InsertTaskAsync(userId, categoryId, taskId, CancellationToken.None);

        var pastDeadline = DateTime.UtcNow.AddMinutes(-1);

        var result = await controller.Update(
            new UpdateTaskInputModel
            {
                Id = taskId.ToString(),
                CategoryId = categoryId.ToString(),
                Description = "Valid",
                Deadline = pastDeadline,
                Priority = TaskPriority.Medium,
                Status = TaskStatus.Todo,
                EstimatedMinutes = 10
            }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var errors = Assert.IsAssignableFrom<List<ValidationFailure>>(badRequest.Value);
        Assert.Contains(errors, e => e.PropertyName == "Deadline");
    }

    [Fact]
    public async Task Update_ShouldReturn_BadRequest_When_EstimatedMinutes_Is_Negative()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        await InsertTaskAsync(userId, categoryId, taskId, CancellationToken.None);

        var result = await controller.Update(
            new UpdateTaskInputModel
            {
                Id = taskId.ToString(),
                CategoryId = categoryId.ToString(),
                Description = "Valid",
                Deadline = DateTime.UtcNow.AddDays(1),
                Priority = TaskPriority.Medium,
                Status = TaskStatus.Todo,
                EstimatedMinutes = -1
            }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var errors = Assert.IsAssignableFrom<List<ValidationFailure>>(badRequest.Value);
        Assert.Contains(errors, e => e.PropertyName == "EstimatedMinutes");
    }

    [Fact]
    public async Task Update_ShouldReturn_BadRequest_When_InputModel_Is_Null()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.Update(null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturn_BadRequestObject_When_Id_Is_Invalid()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.Update(
            new UpdateTaskInputModel
            {
                Id = "not-an-object-id", CategoryId = ObjectId.GenerateNewId().ToString(), Description = "Valid"
            }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var errors = Assert.IsAssignableFrom<List<ValidationFailure>>(badRequest.Value);
        Assert.Contains(errors, e => e.PropertyName == nameof(UpdateTaskInputModel.Id));
    }

    [Fact]
    public async Task Update_ShouldReturn_BadRequestObject_When_CategoryId_Is_Invalid()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.Update(
            new UpdateTaskInputModel
            {
                Id = ObjectId.GenerateNewId().ToString(), CategoryId = "not-an-object-id", Description = "Valid"
            }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var errors = Assert.IsAssignableFrom<List<ValidationFailure>>(badRequest.Value);
        Assert.Contains(errors, e => e.PropertyName == nameof(UpdateTaskInputModel.CategoryId));
    }

    [Fact]
    public async Task Update_ShouldReturn_Unauthorized_When_User_Is_Not_Authenticated()
    {
        var controller = CreateUnauthenticatedController();

        var result = await controller.Update(
            new UpdateTaskInputModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                CategoryId = ObjectId.GenerateNewId().ToString(),
                Description = "Update"
            }, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldReturn_NotFound_When_Task_Does_Not_Exist_For_User()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var result = await controller.Update(
            new UpdateTaskInputModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                CategoryId = categoryId.ToString(),
                Description = "Update",
                Priority = TaskPriority.High,
                Status = TaskStatus.InProgress,
                EstimatedMinutes = 20,
                Deadline = DateTime.UtcNow.AddDays(1)
            }, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Update_ShouldUpdate_Task_ForUser_And_Return_ResponseModel()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        var seed = await InsertTaskAsync(userId, categoryId, taskId, CancellationToken.None);

        var newDeadline = DateTime.UtcNow.AddDays(5);

        var result = await controller.Update(
            new UpdateTaskInputModel
            {
                Id = taskId.ToString(),
                CategoryId = categoryId.ToString(),
                Description = "Updated task",
                Deadline = newDeadline,
                Priority = TaskPriority.High,
                Status = TaskStatus.Done,
                EstimatedMinutes = 120
            }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var response = Assert.IsType<TaskResponseModel>(ok.Value);
        Assert.Equal(taskId.ToString(), response.Id);
        Assert.Equal(userId.ToString(), response.UserId);
        Assert.Equal(categoryId.ToString(), response.CategoryId);
        Assert.Equal("Updated task", response.Description);
        Assert.Equal(TaskPriority.High, response.Priority);
        Assert.Equal(120, response.EstimatedMinutes);

        Assert.NotNull(response.Deadline);
        var diffResp = (response.Deadline.Value - newDeadline).Duration();
        Assert.True(diffResp < TimeSpan.FromMilliseconds(1), $"Deadline differs by {diffResp.TotalMilliseconds} ms");

        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var get = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskId),
            CancellationToken.None);
        Assert.True(get.Success);
        Assert.NotNull(get.ResultObject);

        var updated = get.ResultObject!;
        Assert.Equal(userId, updated.UserId);
        Assert.Equal(categoryId, updated.CategoryId);
        Assert.Equal("Updated task", updated.Description);
        Assert.Equal(TaskPriority.High, updated.Priority);
        Assert.Equal(TaskStatus.Done, updated.Status);
        Assert.Equal(120, updated.EstimatedMinutes);

        Assert.NotNull(updated.Deadline);
        var diffDb = (updated.Deadline.Value - newDeadline).Duration();
        Assert.True(diffDb < TimeSpan.FromMilliseconds(1), $"Deadline differs by {diffDb.TotalMilliseconds} ms");

        var createdAtDiff = (updated.CreatedAt - seed.CreatedAt).Duration();
        Assert.True(createdAtDiff < TimeSpan.FromMilliseconds(1),
            $"CreatedAt differs by {createdAtDiff.TotalMilliseconds} ms");

        Assert.NotNull(updated.UpdatedAt);
        Assert.True(updated.UpdatedAt.Value >= seed.UpdatedAt!.Value);
    }

    [Fact]
    public async Task Update_ShouldReturn_NotFound_When_Task_Belongs_To_Different_User()
    {
        var controllerA = CreateAuthenticatedController(out var userA);
        var controllerB = CreateAuthenticatedController(out var userB);

        var categoryA = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userA, categoryA, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        await InsertTaskAsync(userA, categoryA, taskId, CancellationToken.None);

        var result = await controllerB.Update(
            new UpdateTaskInputModel
            {
                Id = taskId.ToString(),
                CategoryId = categoryA.ToString(),
                Description = "Attempted update",
                Priority = TaskPriority.High,
                Status = TaskStatus.InProgress
            }, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);

        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var get = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskId),
            CancellationToken.None);
        Assert.True(get.Success);
        Assert.NotNull(get.ResultObject);
        Assert.Equal(userA, get.ResultObject!.UserId);
        Assert.Equal("Seed task", get.ResultObject.Description);
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturn_BadRequest_When_TaskIds_Is_Null()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.UpdateStatus(null, TaskStatus.Done, TaskStatus.InProgress, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturn_BadRequest_When_TaskIds_Is_Empty()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.UpdateStatus(Array.Empty<ObjectId>(), TaskStatus.Done, TaskStatus.InProgress, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturn_Unauthorized_When_User_Is_Not_Authenticated()
    {
        var controller = CreateUnauthenticatedController();

        var result = await controller.UpdateStatus(new[] { ObjectId.GenerateNewId() }, TaskStatus.Done, TaskStatus.InProgress,
            CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task
        UpdateStatus_ShouldReturn_NotFound_When_Some_Tasks_Do_Not_Exist_For_User_And_Should_Not_Update_Any()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var existingTaskId = ObjectId.GenerateNewId();
        var seed = await InsertTaskAsync(userId, categoryId, existingTaskId, CancellationToken.None);

        var missingTaskId = ObjectId.GenerateNewId();

        var result = await controller.UpdateStatus(new[] { existingTaskId, missingTaskId }, TaskStatus.Done, TaskStatus.InProgress,
            CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);

        // Ensure existing task was NOT updated (method should short-circuit before updating)
        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var get = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, existingTaskId),
            CancellationToken.None);
        Assert.True(get.Success);
        Assert.NotNull(get.ResultObject);

        var dbTask = get.ResultObject!;
        Assert.Equal(TaskStatus.Todo, dbTask.Status);
        Assert.True(dbTask.UpdatedAt.HasValue);
        AssertUtcClose(seed.UpdatedAt!.Value, dbTask.UpdatedAt!.Value);
    }

    [Fact]
    public async Task
        UpdateStatus_ShouldReturn_NotFound_When_A_Task_Belongs_To_Different_User_And_Should_Not_Update_Any()
    {
        var controllerA = CreateAuthenticatedController(out var userA);
        var controllerB = CreateAuthenticatedController(out var userB);

        var categoryA = ObjectId.GenerateNewId();
        var categoryB = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userA, categoryA, CancellationToken.None);
        await InsertCategoryAsync(userB, categoryB, CancellationToken.None);

        var taskAId = ObjectId.GenerateNewId();
        var taskBId = ObjectId.GenerateNewId();

        var seedA = await InsertTaskAsync(userA, categoryA, taskAId, CancellationToken.None);
        var seedB = await InsertTaskAsync(userB, categoryB, taskBId, CancellationToken.None);

        // userA tries to update both tasks (one belongs to userB)
        var result = await controllerA.UpdateStatus(
            new[] { taskAId, taskBId }, TaskStatus.Done, TaskStatus.InProgress, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);

        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);

        var getA = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskAId),
            CancellationToken.None);

        Assert.True(getA.Success);
        Assert.NotNull(getA.ResultObject);
        Assert.Equal(TaskStatus.Todo, getA.ResultObject!.Status);

        AssertUtcClose(seedA.UpdatedAt!.Value, getA.ResultObject!.UpdatedAt!.Value);

        var getB = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskBId),
            CancellationToken.None);

        Assert.True(getB.Success);
        Assert.NotNull(getB.ResultObject);
        Assert.Equal(TaskStatus.Todo, getB.ResultObject!.Status);

        AssertUtcClose(seedB.UpdatedAt!.Value, getB.ResultObject!.UpdatedAt!.Value);
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturn_Ok_With_ModifiedCount_And_Update_Status_For_All_Tasks()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var taskId1 = ObjectId.GenerateNewId();
        var taskId2 = ObjectId.GenerateNewId();

        var seed1 = await InsertTaskAsync(userId, categoryId, taskId1, CancellationToken.None);
        var seed2 = await InsertTaskAsync(userId, categoryId, taskId2, CancellationToken.None);

        var result = await controller.UpdateStatus(new[] { taskId1, taskId2 }, TaskStatus.Done, TaskStatus.InProgress, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var modifiedCount = Assert.IsType<long>(ok.Value);
        Assert.Equal(2, modifiedCount);

        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);

        var get1 = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskId1),
            CancellationToken.None);
        Assert.True(get1.Success);
        Assert.NotNull(get1.ResultObject);
        Assert.Equal(TaskStatus.Done, get1.ResultObject!.Status);
        Assert.True(get1.ResultObject!.UpdatedAt!.Value >= seed1.UpdatedAt!.Value);

        var get2 = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskId2),
            CancellationToken.None);
        Assert.True(get2.Success);
        Assert.NotNull(get2.ResultObject);
        Assert.Equal(TaskStatus.Done, get2.ResultObject!.Status);
        Assert.True(get2.ResultObject!.UpdatedAt!.Value >= seed2.UpdatedAt!.Value);
    }

    [Fact]
    public async Task
        UpdateStatus_ShouldReturn_NotFound_When_TaskIds_Contains_Empty_ObjectId_And_Should_Not_Update_Any()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var existingTaskId = ObjectId.GenerateNewId();
        var seed = await InsertTaskAsync(userId, categoryId, existingTaskId, CancellationToken.None);

        // ObjectId.Empty will never exist in DB, so service should return NotFound and not update
        var result = await controller.UpdateStatus(new[] { existingTaskId, ObjectId.Empty }, TaskStatus.Done, TaskStatus.InProgress,
            CancellationToken.None);

        var notFound = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, notFound.StatusCode);

        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var get = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, existingTaskId),
            CancellationToken.None);
        Assert.True(get.Success);
        Assert.NotNull(get.ResultObject);

        var dbTask = get.ResultObject!;
        Assert.Equal(TaskStatus.Todo, dbTask.Status);
        AssertUtcClose(seed.UpdatedAt!.Value, dbTask.UpdatedAt!.Value);
    }

    [Fact]
    public async Task UpdateStatusOne_ShouldReturn_BadRequest_When_TaskId_Is_Empty()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.UpdateStatusOne(ObjectId.Empty, TaskStatus.Done, TaskStatus.InProgress, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task UpdateStatusOne_ShouldReturn_Unauthorized_When_User_Is_Not_Authenticated()
    {
        var controller = CreateUnauthenticatedController();

        var result =
            await controller.UpdateStatusOne(ObjectId.GenerateNewId(), TaskStatus.Done, TaskStatus.InProgress, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task UpdateStatusOne_ShouldReturn_NotFound_When_Task_Does_Not_Exist_For_User()
    {
        var controller = CreateAuthenticatedController(out _);

        var result =
            await controller.UpdateStatusOne(ObjectId.GenerateNewId(), TaskStatus.Done, TaskStatus.InProgress, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task
        UpdateStatusOne_ShouldReturn_NotFound_When_Task_Belongs_To_Different_User_And_Should_Not_Update_Task()
    {
        var controllerA = CreateAuthenticatedController(out var userA);
        var controllerB = CreateAuthenticatedController(out var userB);

        var categoryA = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userA, categoryA, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        var seed = await InsertTaskAsync(userA, categoryA, taskId, CancellationToken.None);

        var result = await controllerB.UpdateStatusOne(taskId, TaskStatus.Done, TaskStatus.InProgress, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);

        // Ensure task remains unchanged
        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var get = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskId),
            CancellationToken.None);

        Assert.True(get.Success);
        Assert.NotNull(get.ResultObject);
        Assert.Equal(userA, get.ResultObject!.UserId);
        Assert.Equal(TaskStatus.Todo, get.ResultObject!.Status);
        AssertUtcClose(seed.UpdatedAt!.Value, get.ResultObject!.UpdatedAt!.Value);
    }

    [Fact]
    public async Task UpdateStatusOne_ShouldReturn_Ok_With_ModifiedCount_And_Update_Status_For_Task()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        var seed = await InsertTaskAsync(userId, categoryId, taskId, CancellationToken.None);

        var result = await controller.UpdateStatusOne(taskId, TaskStatus.Done, TaskStatus.InProgress, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var modifiedCount = Assert.IsType<long>(ok.Value);
        Assert.Equal(1, modifiedCount);

        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var get = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskId),
            CancellationToken.None);

        Assert.True(get.Success);
        Assert.NotNull(get.ResultObject);
        Assert.Equal(TaskStatus.Done, get.ResultObject!.Status);
        Assert.True(get.ResultObject!.UpdatedAt!.Value >= seed.UpdatedAt!.Value);
    }

    [Fact]
    public async Task GetOne_ShouldReturn_BadRequest_When_Id_Is_NullOrWhitespace()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.GetOne("", null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetOne_ShouldReturn_Unauthorized_When_User_Is_Not_Authenticated()
    {
        var controller = CreateUnauthenticatedController();

        var result = await controller.GetOne(ObjectId.GenerateNewId().ToString(), null, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task GetOne_ShouldReturn_NotFound_When_Task_Does_Not_Exist_For_User()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.GetOne(ObjectId.GenerateNewId().ToString(), null, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task GetOne_ShouldReturn_NotFound_When_Task_Belongs_To_Different_User()
    {
        var controllerA = CreateAuthenticatedController(out var userA);
        var controllerB = CreateAuthenticatedController(out var userB);

        var categoryA = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userA, categoryA, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        await InsertTaskAsync(userA, categoryA, taskId, CancellationToken.None);

        var result = await controllerB.GetOne(taskId.ToString(), null, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task GetOne_ShouldReturn_BadRequest_When_CategoryId_Is_Invalid()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.GetOne(ObjectId.GenerateNewId().ToString(), "not-an-object-id",
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetOne_ShouldReturn_Ok_When_Task_Exists_For_User()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        await InsertTaskAsync(userId, categoryId, taskId, CancellationToken.None);

        var result = await controller.GetOne(taskId.ToString(), null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var response = Assert.IsType<TaskResponseModel>(ok.Value);
        Assert.Equal(taskId.ToString(), response.Id);
        Assert.Equal(userId.ToString(), response.UserId);
        Assert.Equal(categoryId.ToString(), response.CategoryId);
        Assert.Equal("Seed task", response.Description);
    }

    [Fact]
    public async Task GetOne_ShouldReturn_NotFound_When_CategoryId_Is_Provided_But_Task_Is_In_Different_Category()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryA = ObjectId.GenerateNewId();
        var categoryB = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryA, CancellationToken.None);
        await InsertCategoryAsync(userId, categoryB, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        await InsertTaskAsync(userId, categoryA, taskId, CancellationToken.None);

        var result = await controller.GetOne(taskId.ToString(), categoryB.ToString(), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task GetOne_ShouldReturn_Ok_When_CategoryId_Is_Provided_And_Task_Is_In_That_Category()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        await InsertTaskAsync(userId, categoryId, taskId, CancellationToken.None);

        var result = await controller.GetOne(taskId.ToString(), categoryId.ToString(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var response = Assert.IsType<TaskResponseModel>(ok.Value);
        Assert.Equal(taskId.ToString(), response.Id);
        Assert.Equal(categoryId.ToString(), response.CategoryId);
    }

    [Fact]
    public async Task GetMany_ShouldReturn_Unauthorized_When_User_Is_Not_Authenticated()
    {
        var controller = CreateUnauthenticatedController();

        var result = await controller.GetMany(null, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task GetMany_ShouldReturn_BadRequest_When_CategoryId_Is_Invalid()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.GetMany("not-an-object-id", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetMany_ShouldReturn_Ok_And_Empty_List_When_User_Has_No_Tasks()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.GetMany(null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var response = Assert.IsType<List<TaskResponseModel>>(ok.Value);
        Assert.Empty(response);
    }

    [Fact]
    public async Task GetMany_ShouldReturn_Ok_And_All_Tasks_For_User_When_CategoryId_Is_Not_Provided()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryA = ObjectId.GenerateNewId();
        var categoryB = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryA, CancellationToken.None);
        await InsertCategoryAsync(userId, categoryB, CancellationToken.None);

        var taskA = ObjectId.GenerateNewId();
        var taskB = ObjectId.GenerateNewId();
        await InsertTaskAsync(userId, categoryA, taskA, CancellationToken.None);
        await InsertTaskAsync(userId, categoryB, taskB, CancellationToken.None);

        // Noise from another user
        var otherUser = ObjectId.GenerateNewId();
        var otherCategory = ObjectId.GenerateNewId();
        await InsertCategoryAsync(otherUser, otherCategory, CancellationToken.None);
        await InsertTaskAsync(otherUser, otherCategory, ObjectId.GenerateNewId(), CancellationToken.None);

        var result = await controller.GetMany(null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var response = Assert.IsType<List<TaskResponseModel>>(ok.Value);
        Assert.Equal(2, response.Count);

        var ids = response.Select(x => x.Id).ToHashSet();
        Assert.Contains(taskA.ToString(), ids);
        Assert.Contains(taskB.ToString(), ids);
    }

    [Fact]
    public async Task GetMany_ShouldReturn_Ok_And_Filter_By_Category_When_CategoryId_Is_Provided()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryA = ObjectId.GenerateNewId();
        var categoryB = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryA, CancellationToken.None);
        await InsertCategoryAsync(userId, categoryB, CancellationToken.None);

        var taskA1 = ObjectId.GenerateNewId();
        var taskA2 = ObjectId.GenerateNewId();
        var taskB1 = ObjectId.GenerateNewId();

        await InsertTaskAsync(userId, categoryA, taskA1, CancellationToken.None);
        await InsertTaskAsync(userId, categoryA, taskA2, CancellationToken.None);
        await InsertTaskAsync(userId, categoryB, taskB1, CancellationToken.None);

        var result = await controller.GetMany(categoryA.ToString(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var response = Assert.IsType<List<TaskResponseModel>>(ok.Value);
        Assert.Equal(2, response.Count);

        Assert.All(response, t => Assert.Equal(categoryA.ToString(), t.CategoryId));

        var ids = response.Select(x => x.Id).ToHashSet();
        Assert.Contains(taskA1.ToString(), ids);
        Assert.Contains(taskA2.ToString(), ids);
        Assert.DoesNotContain(taskB1.ToString(), ids);
    }

    [Fact]
    public async Task DeleteOne_ShouldReturn_BadRequest_When_Id_Is_NullOrWhitespace()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.DeleteOne("", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task DeleteOne_ShouldReturn_BadRequest_When_Id_Is_Invalid()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.DeleteOne("not-an-object-id", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task DeleteOne_ShouldReturn_BadRequest_When_Id_Is_ObjectId_Empty()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.DeleteOne(ObjectId.Empty.ToString(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task DeleteOne_ShouldReturn_Unauthorized_When_User_Is_Not_Authenticated()
    {
        var controller = CreateUnauthenticatedController();

        var result = await controller.DeleteOne(ObjectId.GenerateNewId().ToString(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task DeleteOne_ShouldReturn_NotFound_When_Task_Does_Not_Exist_For_User()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.DeleteOne(ObjectId.GenerateNewId().ToString(), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task DeleteOne_ShouldReturn_NotFound_When_Task_Belongs_To_Different_User_And_Should_Not_Delete_It()
    {
        var controllerA = CreateAuthenticatedController(out var userA);
        var controllerB = CreateAuthenticatedController(out var userB);

        var categoryA = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userA, categoryA, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        await InsertTaskAsync(userA, categoryA, taskId, CancellationToken.None);

        var result = await controllerB.DeleteOne(taskId.ToString(), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);

        // Ensure task still exists
        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var get = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskId),
            CancellationToken.None);
        Assert.True(get.Success);
        Assert.NotNull(get.ResultObject);
        Assert.Equal(userA, get.ResultObject!.UserId);
    }

    [Fact]
    public async Task DeleteOne_ShouldReturn_Ok_And_Delete_Task_For_User()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        await InsertTaskAsync(userId, categoryId, taskId, CancellationToken.None);

        var result = await controller.DeleteOne(taskId.ToString(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var response = Assert.IsType<TaskResponseModel>(ok.Value);
        Assert.Equal(taskId.ToString(), response.Id);
        Assert.Equal(userId.ToString(), response.UserId);

        // Ensure task is removed from DB
        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var get = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskId),
            CancellationToken.None);

        Assert.False(get.Success);
        Assert.Null(get
            .ResultObject); // GetOneAsync returns Success=false with NotFoundError in your repo? If so adjust below

        // If your repo returns Success=false on not found:
        // Assert.False(get.Success);
        // Assert.Contains(get.Errors, e => e is NotFoundError);
    }

    [Fact]
    public async Task DeleteMany_ShouldReturn_BadRequest_When_TaskIds_Is_Null()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.DeleteMany(null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task DeleteMany_ShouldReturn_BadRequest_When_TaskIds_Is_Empty()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.DeleteMany(Array.Empty<ObjectId>(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task DeleteMany_ShouldReturn_BadRequest_When_TaskIds_Contains_Empty_ObjectId()
    {
        var controller = CreateAuthenticatedController(out _);

        var result =
            await controller.DeleteMany(new[] { ObjectId.GenerateNewId(), ObjectId.Empty }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task DeleteMany_ShouldReturn_Unauthorized_When_User_Is_Not_Authenticated()
    {
        var controller = CreateUnauthenticatedController();

        var result = await controller.DeleteMany(new[] { ObjectId.GenerateNewId() }, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task DeleteMany_ShouldReturn_NotFound_When_Some_Tasks_Do_Not_Exist_For_User_And_Should_Not_Delete_Any()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var existingTaskId = ObjectId.GenerateNewId();
        await InsertTaskAsync(userId, categoryId, existingTaskId, CancellationToken.None);

        var missingTaskId = ObjectId.GenerateNewId();

        var result = await controller.DeleteMany(new[] { existingTaskId, missingTaskId }, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);

        // Ensure existing task still exists
        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var get = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, existingTaskId),
            CancellationToken.None);
        Assert.True(get.Success);
        Assert.NotNull(get.ResultObject);
        Assert.Equal(userId, get.ResultObject!.UserId);
    }

    [Fact]
    public async Task DeleteMany_ShouldReturn_NotFound_When_A_Task_Belongs_To_Different_User_And_Should_Not_Delete_Any()
    {
        var controllerA = CreateAuthenticatedController(out var userA);
        var controllerB = CreateAuthenticatedController(out var userB);

        var categoryA = ObjectId.GenerateNewId();
        var categoryB = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userA, categoryA, CancellationToken.None);
        await InsertCategoryAsync(userB, categoryB, CancellationToken.None);

        var taskAId = ObjectId.GenerateNewId();
        var taskBId = ObjectId.GenerateNewId();

        await InsertTaskAsync(userA, categoryA, taskAId, CancellationToken.None);
        await InsertTaskAsync(userB, categoryB, taskBId, CancellationToken.None);

        // userA tries to delete both tasks, one belongs to userB -> should fail and delete nothing
        var result = await controllerA.DeleteMany(new[] { taskAId, taskBId }, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);

        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);

        var getA = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskAId),
            CancellationToken.None);
        Assert.True(getA.Success);
        Assert.NotNull(getA.ResultObject);

        var getB = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskBId),
            CancellationToken.None);
        Assert.True(getB.Success);
        Assert.NotNull(getB.ResultObject);
    }

    [Fact]
    public async Task DeleteMany_ShouldReturn_Ok_With_DeletedCount_And_Delete_All_Tasks()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryId = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryId, CancellationToken.None);

        var taskId1 = ObjectId.GenerateNewId();
        var taskId2 = ObjectId.GenerateNewId();
        await InsertTaskAsync(userId, categoryId, taskId1, CancellationToken.None);
        await InsertTaskAsync(userId, categoryId, taskId2, CancellationToken.None);

        var result = await controller.DeleteMany(new[] { taskId1, taskId2 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var deletedCount = Assert.IsType<long>(ok.Value);
        Assert.Equal(2, deletedCount);

        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var get1 = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskId1),
            CancellationToken.None);
        var get2 = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskId2),
            CancellationToken.None);

        // Depending on GetOneAsync semantics (see note in DeleteOne test)
        Assert.False(get1.Success);
        Assert.False(get2.Success);
    }

    [Fact]
    public async Task DeleteByCategory_ShouldReturn_BadRequest_When_CategoryId_Is_Empty()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.DeleteByCategory(ObjectId.Empty, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task DeleteByCategory_ShouldReturn_Unauthorized_When_User_Is_Not_Authenticated()
    {
        var controller = CreateUnauthenticatedController();

        var result = await controller.DeleteByCategory(ObjectId.GenerateNewId(), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task DeleteByCategory_ShouldReturn_NotFound_When_Category_Does_Not_Exist_For_User()
    {
        var controller = CreateAuthenticatedController(out _);

        var result = await controller.DeleteByCategory(ObjectId.GenerateNewId(), CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task
        DeleteByCategory_ShouldReturn_NotFound_When_Category_Belongs_To_Different_User_And_Should_Not_Delete_Anything()
    {
        var controllerA = CreateAuthenticatedController(out var userA);
        var controllerB = CreateAuthenticatedController(out var userB);

        var categoryA = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userA, categoryA, CancellationToken.None);

        var taskId = ObjectId.GenerateNewId();
        await InsertTaskAsync(userA, categoryA, taskId, CancellationToken.None);

        var result = await controllerB.DeleteByCategory(categoryA, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);

        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var get = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, taskId),
            CancellationToken.None);
        Assert.True(get.Success);
        Assert.NotNull(get.ResultObject);
    }

    [Fact]
    public async Task DeleteByCategory_ShouldReturn_Ok_With_DeletedCount_And_Delete_All_Tasks_In_Category()
    {
        var controller = CreateAuthenticatedController(out var userId);

        var categoryA = ObjectId.GenerateNewId();
        var categoryB = ObjectId.GenerateNewId();
        await InsertCategoryAsync(userId, categoryA, CancellationToken.None);
        await InsertCategoryAsync(userId, categoryB, CancellationToken.None);

        var a1 = ObjectId.GenerateNewId();
        var a2 = ObjectId.GenerateNewId();
        var b1 = ObjectId.GenerateNewId();

        await InsertTaskAsync(userId, categoryA, a1, CancellationToken.None);
        await InsertTaskAsync(userId, categoryA, a2, CancellationToken.None);
        await InsertTaskAsync(userId, categoryB, b1, CancellationToken.None);

        var result = await controller.DeleteByCategory(categoryA, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var deletedCount = Assert.IsType<long>(ok.Value);
        Assert.Equal(2, deletedCount);

        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);

        var getA1 = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, a1),
            CancellationToken.None);
        var getA2 = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, a2),
            CancellationToken.None);
        var getB1 = await taskRepository.GetOneAsync(Builders<TaskEntity>.Filter.Eq(x => x.Id, b1),
            CancellationToken.None);

        Assert.False(getA1.Success);
        Assert.False(getA2.Success);

        Assert.True(getB1.Success);
        Assert.NotNull(getB1.ResultObject);
    }
    
    [Fact]
    public async Task DeleteManyByUserId_ShouldReturn_Unauthorized_When_User_Is_Not_Authenticated()
    {
        var controller = CreateUnauthenticatedController();
    
        var result = await controller.DeleteMany(CancellationToken.None);
    
        var unauthorized = Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }
    
    [Fact]
    public async Task DeleteManyByUserId_ShouldReturn_Ok_When_User_Has_No_Tasks()
    {
        var controller = CreateAuthenticatedController(out var userId);
    
        // ensure user exists but has no tasks
        var result = await controller.DeleteMany(CancellationToken.None);
    
        var notFound = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, notFound.StatusCode);
    
        // sanity: still no tasks
        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var get = await taskRepository.GetAsync(Builders<TaskEntity>.Filter.Eq(x => x.UserId, userId), CancellationToken.None);
        Assert.True(get.Success);
        Assert.Empty(get.ResultObject);
    }
    
    [Fact]
    public async Task DeleteManyByUserId_ShouldReturn_Ok_With_DeletedCount_And_Delete_All_User_Tasks()
    {
        var controller = CreateAuthenticatedController(out var userId);
    
        var categoryA = ObjectId.GenerateNewId();
        var categoryB = ObjectId.GenerateNewId();
    
        await InsertCategoryAsync(userId, categoryA, CancellationToken.None);
        await InsertCategoryAsync(userId, categoryB, CancellationToken.None);
    
        var t1 = ObjectId.GenerateNewId();
        var t2 = ObjectId.GenerateNewId();
        var t3 = ObjectId.GenerateNewId();
    
        await InsertTaskAsync(userId, categoryA, t1, CancellationToken.None);
        await InsertTaskAsync(userId, categoryA, t2, CancellationToken.None);
        await InsertTaskAsync(userId, categoryB, t3, CancellationToken.None);
    
        var result = await controller.DeleteMany(CancellationToken.None);
    
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    
        var deletedCount = Assert.IsType<long>(ok.Value);
        Assert.Equal(3, deletedCount);
    
        // Verify DB: no tasks remain for userId
        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
        var get = await taskRepository.GetAsync(Builders<TaskEntity>.Filter.Eq(x => x.UserId, userId), CancellationToken.None);
        Assert.True(get.Success);
        Assert.Empty(get.ResultObject);
    }
    
    [Fact]
    public async Task DeleteManyByUserId_Should_Not_Delete_Other_Users_Tasks()
    {
        var controllerA = CreateAuthenticatedController(out var userA);
        var controllerB = CreateAuthenticatedController(out var userB);
    
        var categoryA = ObjectId.GenerateNewId();
        var categoryB = ObjectId.GenerateNewId();
    
        await InsertCategoryAsync(userA, categoryA, CancellationToken.None);
        await InsertCategoryAsync(userB, categoryB, CancellationToken.None);
    
        var a1 = ObjectId.GenerateNewId();
        var a2 = ObjectId.GenerateNewId();
        var b1 = ObjectId.GenerateNewId();
    
        await InsertTaskAsync(userA, categoryA, a1, CancellationToken.None);
        await InsertTaskAsync(userA, categoryA, a2, CancellationToken.None);
        await InsertTaskAsync(userB, categoryB, b1, CancellationToken.None);
    
        var result = await controllerA.DeleteMany(CancellationToken.None);
    
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    
        var deletedCount = Assert.IsType<long>(ok.Value);
        Assert.Equal(2, deletedCount);
    
        var taskRepository = TestPreparationData.CreateRepository<TaskEntity>(this._mongoFixture);
    
        // userA tasks removed
        var getA = await taskRepository.GetAsync(Builders<TaskEntity>.Filter.Eq(x => x.UserId, userA), CancellationToken.None);
        Assert.True(getA.Success);
        Assert.Empty(getA.ResultObject);
    
        // userB task remains
        var getB = await taskRepository.GetAsync(Builders<TaskEntity>.Filter.Eq(x => x.UserId, userB), CancellationToken.None);
        Assert.True(getB.Success);
        Assert.Single(getB.ResultObject);
        Assert.Equal(b1, getB.ResultObject[0].Id);
    }

    private static void AssertUtcClose(DateTime expected, DateTime actual, double maxMs = 1)
    {
        var diff = (actual - expected).Duration();
        Assert.True(diff < TimeSpan.FromMilliseconds(maxMs),
            $"Expected {expected:o} but got {actual:o}. Diff={diff.TotalMilliseconds}ms");
    }
}