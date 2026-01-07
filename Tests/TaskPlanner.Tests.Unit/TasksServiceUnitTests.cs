using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Task;
using TaskPlanner.API.Core.Services;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;
using TaskStatus = TaskPlanner.API.Data.Models.TaskStatus;
using Data = TaskPlanner.API.Data;

namespace TaskPlanner.Tests.Unit;

public class TaskServiceTests
{
    private static (ITaskService Service, Mock<IBaseRepository<Data.Models.Task>> TaskRepo, Mock<IBaseRepository<Category>> CategoryRepo) CreateSut()
    {
        var taskRepo = new Mock<IBaseRepository<Data.Models.Task>>(MockBehavior.Strict);
        var categoryRepo = new Mock<IBaseRepository<Category>>(MockBehavior.Strict);
        var sut = new TaskService(taskRepo.Object, categoryRepo.Object);
        return (sut, taskRepo, categoryRepo);
    }

    private static BsonDocument RenderFilter<T>(FilterDefinition<T> filter)
    {
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
        var args = new RenderArgs<T>(serializer, BsonSerializer.SerializerRegistry);
        return filter.Render(args);
    }

    private static BsonDocument RenderSort<T>(SortDefinition<T> sort)
    {
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
        var args = new RenderArgs<T>(serializer, BsonSerializer.SerializerRegistry);
        return sort.Render(args);
    }

    private static BsonDocument RenderUpdate<T>(UpdateDefinition<T> update)
    {
        var args = new RenderArgs<T>
        {
            DocumentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<T>(),
            SerializerRegistry = BsonSerializer.SerializerRegistry
        };

        var rendered = update.Render(args);
        return rendered.AsBsonDocument;
    }

    #region CreateTask Tests

    [Fact]
    public async System.Threading.Tasks.Task CreateTask_ShouldReturnError_When_UserId_IsInvalid()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var input = new CreateTaskInputModel { Description = "Test task" };
        var result = await sut.CreateTask(input, ObjectId.Empty, ObjectId.GenerateNewId(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid user id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTask_ShouldReturnError_When_CategoryId_IsInvalid()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var input = new CreateTaskInputModel { Description = "Test task" };
        var result = await sut.CreateTask(input, ObjectId.GenerateNewId(), ObjectId.Empty, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid category id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTask_ShouldReturnError_When_Category_DoesNotExist()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var categoryId = ObjectId.GenerateNewId();

        categoryRepo.Setup(x => x.AnyAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<bool>().WithRelatedObject(false));

        var input = new CreateTaskInputModel { Description = "Test task" };
        var result = await sut.CreateTask(input, userId, categoryId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains($"Category with id {categoryId} not found."));
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTask_ShouldUseDefaultValues_When_NotProvided()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var categoryId = ObjectId.GenerateNewId();

        categoryRepo.Setup(x => x.AnyAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<bool>().WithRelatedObject(true));

        Data.Models.Task? captured = null;
        taskRepo.Setup(x => x.CreateAsync(It.IsAny<Data.Models.Task>()))
            .Callback<Data.Models.Task>(t => captured = t)
            .ReturnsAsync(new OperationResult<Data.Models.Task>());

        var input = new CreateTaskInputModel { Description = "Test task" };
        await sut.CreateTask(input, userId, categoryId, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("Test task", captured!.Description);
        Assert.Equal(userId, captured.UserId);
        Assert.Equal(categoryId, captured.CategoryId);
        Assert.Equal(TaskPriority.Medium, captured.Priority);
        Assert.Equal(TaskStatus.Todo, captured.Status);
        Assert.Null(captured.EstimatedMinutes);
        Assert.Null(captured.Deadline);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTask_ShouldUseProvidedValues_When_Present()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var categoryId = ObjectId.GenerateNewId();
        var deadline = DateTime.UtcNow.AddDays(7);

        categoryRepo.Setup(x => x.AnyAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<bool>().WithRelatedObject(true));

        Data.Models.Task? captured = null;
        taskRepo.Setup(x => x.CreateAsync(It.IsAny<Data.Models.Task>()))
            .Callback<Data.Models.Task>(t => captured = t)
            .ReturnsAsync(new OperationResult<Data.Models.Task>());

        var input = new CreateTaskInputModel
        {
            Description = "Important task",
            Priority = TaskPriority.High,
            Status = TaskStatus.InProgress,
            Deadline = deadline,
            EstimatedMinutes = 120
        };

        await sut.CreateTask(input, userId, categoryId, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("Important task", captured!.Description);
        Assert.Equal(TaskPriority.High, captured.Priority);
        Assert.Equal(TaskStatus.InProgress, captured.Status);
        Assert.Equal(deadline, captured.Deadline);
        Assert.Equal(120, captured.EstimatedMinutes);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateTask_ShouldVerifyCategory_WithCorrectFilter()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var categoryId = ObjectId.GenerateNewId();

        FilterDefinition<Category>? capturedFilter = null;

        categoryRepo.Setup(x => x.AnyAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<Category>, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync(new OperationResult<bool>().WithRelatedObject(true));

        taskRepo.Setup(x => x.CreateAsync(It.IsAny<Data.Models.Task>()))
            .ReturnsAsync(new OperationResult<Data.Models.Task>());

        var input = new CreateTaskInputModel { Description = "Test" };
        await sut.CreateTask(input, userId, categoryId, CancellationToken.None);

        Assert.NotNull(capturedFilter);
        var rendered = RenderFilter(capturedFilter!);

        if (rendered.Contains("$and"))
        {
            var andArray = rendered["$and"].AsBsonArray;
            Assert.Contains(andArray, x => x.AsBsonDocument.Contains("_id") && x.AsBsonDocument["_id"].AsObjectId == categoryId);
            Assert.Contains(andArray, x => x.AsBsonDocument.Contains("UserId") && x.AsBsonDocument["UserId"].AsObjectId == userId);
        }
        else
        {
            Assert.Equal(categoryId, rendered["_id"].AsObjectId);
            Assert.Equal(userId, rendered["UserId"].AsObjectId);
        }
    }

    #endregion

    #region UpdateTask Tests

    [Fact]
    public async System.Threading.Tasks.Task UpdateTask_ShouldReturnError_When_UserId_IsInvalid()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var input = new UpdateTaskInputModel
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CategoryId = ObjectId.GenerateNewId().ToString(),
            Description = "Updated"
        };

        var result = await sut.UpdateTask(input, ObjectId.Empty, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid user id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateTask_ShouldReturnError_When_Category_DoesNotExist()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var categoryId = ObjectId.GenerateNewId();

        categoryRepo.Setup(x => x.AnyAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<bool>().WithRelatedObject(false));

        var input = new UpdateTaskInputModel
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CategoryId = categoryId.ToString(),
            Description = "Updated"
        };

        var result = await sut.UpdateTask(input, userId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e is NotFoundError);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateTask_ShouldReturnError_When_Task_DoesNotExist()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var categoryId = ObjectId.GenerateNewId();

        categoryRepo.Setup(x => x.AnyAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<bool>().WithRelatedObject(true));

        taskRepo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<Data.Models.Task>().WithRelatedObject(null as Data.Models.Task));

        var input = new UpdateTaskInputModel
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CategoryId = categoryId.ToString(),
            Description = "Updated"
        };

        var result = await sut.UpdateTask(input, userId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Entity not found."));
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateTask_ShouldReturnError_When_CategoryId_DoesNotMatch()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var originalCategoryId = ObjectId.GenerateNewId();
        var newCategoryId = ObjectId.GenerateNewId();
        var taskId = ObjectId.GenerateNewId();

        categoryRepo.Setup(x => x.AnyAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<bool>().WithRelatedObject(true));

        var existingTask = new Data.Models.Task
        {
            Id = taskId,
            UserId = userId,
            CategoryId = originalCategoryId,
            Description = "Original"
        };

        taskRepo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<Data.Models.Task>().WithRelatedObject(existingTask));

        var input = new UpdateTaskInputModel
        {
            Id = taskId.ToString(),
            CategoryId = newCategoryId.ToString(),
            Description = "Updated"
        };

        var result = await sut.UpdateTask(input, userId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("is not with the given category id"));
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateTask_ShouldPreserveCreatedAt_And_UpdateUpdatedAt()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var categoryId = ObjectId.GenerateNewId();
        var taskId = ObjectId.GenerateNewId();
        var createdAt = DateTime.UtcNow.AddDays(-5);

        categoryRepo.Setup(x => x.AnyAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<bool>().WithRelatedObject(true));

        var existingTask = new Data.Models.Task
        {
            Id = taskId,
            UserId = userId,
            CategoryId = categoryId,
            Description = "Original",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        taskRepo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<Data.Models.Task>().WithRelatedObject(existingTask));

        Data.Models.Task? captured = null;
        taskRepo.Setup(x => x.UpdateAsync(It.IsAny<Data.Models.Task>(), It.IsAny<CancellationToken>()))
            .Callback<Data.Models.Task, CancellationToken>((t, _) => captured = t)
            .ReturnsAsync(new OperationResult<Data.Models.Task>());

        var input = new UpdateTaskInputModel
        {
            Id = taskId.ToString(),
            CategoryId = categoryId.ToString(),
            Description = "Updated",
            Status = TaskStatus.Done
        };

        await sut.UpdateTask(input, userId, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(createdAt, captured!.CreatedAt);
        Assert.True(captured.UpdatedAt > createdAt);
        Assert.Equal("Updated", captured.Description);
        Assert.Equal(TaskStatus.Done, captured.Status);
    }

    #endregion

    #region ModifyTaskStatus Tests

    [Fact]
    public async System.Threading.Tasks.Task ModifyTaskStatus_ShouldReturnError_When_UserId_IsInvalid()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var result = await sut.ModifyTaskStatus(ObjectId.GenerateNewId(), ObjectId.Empty, TaskStatus.Done, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid user id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task ModifyTaskStatus_ShouldReturnError_When_TaskId_IsInvalid()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var result = await sut.ModifyTaskStatus(ObjectId.Empty, ObjectId.GenerateNewId(), TaskStatus.Done, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid task id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task ModifyTaskStatus_ShouldUpdateStatus_WithCorrectUpdate()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var taskId = ObjectId.GenerateNewId();

        var existingTask = new Data.Models.Task { Id = taskId, UserId = userId, Status = TaskStatus.Todo };

        taskRepo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<Data.Models.Task>().WithRelatedObject(existingTask));

        UpdateDefinition<Data.Models.Task>? capturedUpdate = null;

        taskRepo.Setup(x => x.ModifyManyAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<UpdateDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .Callback<FilterDefinition<Data.Models.Task>, UpdateDefinition<Data.Models.Task>, CancellationToken, bool>((_, u, __, ___) => capturedUpdate = u)
            .ReturnsAsync(new OperationResult<long>().WithRelatedObject(1L));

        await sut.ModifyTaskStatus(taskId, userId, TaskStatus.Done, CancellationToken.None);

        Assert.NotNull(capturedUpdate);
        var rendered = RenderUpdate(capturedUpdate!);

        Assert.True(rendered.Contains("$set"));
        var setDoc = rendered["$set"].AsBsonDocument;
        Assert.Equal("Done", setDoc["Status"]);
        Assert.True(setDoc.Contains("UpdatedAt"));
    }

    #endregion

    #region ModifyManyTaskStatus Tests

    [Fact]
    public async System.Threading.Tasks.Task ModifyManyTaskStatus_ShouldReturnError_When_NoTasks_Provided()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var result = await sut.ModifyManyTaskStatus(Array.Empty<ObjectId>(), ObjectId.GenerateNewId(), TaskStatus.Done, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("No tasks provided."));
    }

    [Fact]
    public async System.Threading.Tasks.Task ModifyManyTaskStatus_ShouldReturnError_When_SomeTasks_NotFound()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var taskId1 = ObjectId.GenerateNewId();
        var taskId2 = ObjectId.GenerateNewId();
        var taskId3 = ObjectId.GenerateNewId();

        var foundTasks = new List<Data.Models.Task>
        {
            new Data.Models.Task { Id = taskId1, UserId = userId },
            new Data.Models.Task { Id = taskId2, UserId = userId }
        };

        taskRepo.Setup(x => x.GetAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>(), null, null, null))
            .ReturnsAsync(new OperationResult<IReadOnlyList<Data.Models.Task>>().WithRelatedObject(foundTasks));

        var result = await sut.ModifyManyTaskStatus(new[] { taskId1, taskId2, taskId3 }, userId, TaskStatus.Done, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e is NotFoundError && e.ToString()!.Contains("Some tasks were not found"));
    }

    [Fact]
    public async System.Threading.Tasks.Task ModifyManyTaskStatus_ShouldUpdate_When_AllTasks_Found()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var taskId1 = ObjectId.GenerateNewId();
        var taskId2 = ObjectId.GenerateNewId();

        var foundTasks = new List<Data.Models.Task>
        {
            new Data.Models.Task { Id = taskId1, UserId = userId },
            new Data.Models.Task { Id = taskId2, UserId = userId }
        };

        taskRepo.Setup(x => x.GetAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>(), null, null, null))
            .ReturnsAsync(new OperationResult<IReadOnlyList<Data.Models.Task>>().WithRelatedObject(foundTasks));

        taskRepo.Setup(x => x.ModifyManyAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<UpdateDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new OperationResult<long>().WithRelatedObject(2L));

        var result = await sut.ModifyManyTaskStatus(new[] { taskId1, taskId2 }, userId, TaskStatus.InProgress, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2L, result.ResultObject);
    }

    #endregion

    #region GetTaskById Tests

    [Fact]
    public async System.Threading.Tasks.Task GetTaskById_ShouldReturnError_When_TaskId_IsInvalid()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var result = await sut.GetTaskById("invalid-id", ObjectId.GenerateNewId(), null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid task id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskById_ShouldReturnError_When_CategoryId_IsInvalid()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var result = await sut.GetTaskById(ObjectId.GenerateNewId().ToString(), ObjectId.GenerateNewId(), "invalid-category", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid category id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskById_ShouldCallRepository_WithFilter_OnIdAndUserId()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var taskId = ObjectId.GenerateNewId();

        FilterDefinition<Data.Models.Task>? capturedFilter = null;

        taskRepo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<Data.Models.Task>, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync(new OperationResult<Data.Models.Task>());

        await sut.GetTaskById(taskId.ToString(), userId, null, CancellationToken.None);

        Assert.NotNull(capturedFilter);
        var rendered = RenderFilter(capturedFilter!);

        if (rendered.Contains("$and"))
        {
            var andArray = rendered["$and"].AsBsonArray;
            Assert.Contains(andArray, x => x.AsBsonDocument.Contains("_id") && x.AsBsonDocument["_id"].AsObjectId == taskId);
            Assert.Contains(andArray, x => x.AsBsonDocument.Contains("UserId") && x.AsBsonDocument["UserId"].AsObjectId == userId);
        }
        else
        {
            Assert.Equal(taskId, rendered["_id"].AsObjectId);
            Assert.Equal(userId, rendered["UserId"].AsObjectId);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskById_ShouldIncludeCategoryFilter_When_CategoryId_Provided()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var taskId = ObjectId.GenerateNewId();
        var categoryId = ObjectId.GenerateNewId();

        FilterDefinition<Data.Models.Task>? capturedFilter = null;

        taskRepo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<Data.Models.Task>, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync(new OperationResult<Data.Models.Task>());

        await sut.GetTaskById(taskId.ToString(), userId, categoryId.ToString(), CancellationToken.None);

        Assert.NotNull(capturedFilter);
        var rendered = RenderFilter(capturedFilter!);

        if (rendered.Contains("$and"))
        {
            var andArray = rendered["$and"].AsBsonArray;
            Assert.Contains(andArray, x => x.AsBsonDocument.Contains("CategoryId") && x.AsBsonDocument["CategoryId"].AsObjectId == categoryId);
        }
        else
        {
            Assert.Equal(categoryId, rendered["CategoryId"].AsObjectId);
        }
    }

    #endregion

    #region GetTasks Tests

    [Fact]
    public async System.Threading.Tasks.Task GetTasks_ShouldReturnError_When_UserId_IsInvalid()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var result = await sut.GetTasks(ObjectId.Empty, null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid user id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTasks_ShouldCallRepository_WithFilter_OnUserId_AndDescendingSort()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();

        FilterDefinition<Data.Models.Task>? capturedFilter = null;
        SortDefinition<Data.Models.Task>? capturedSort = null;

        taskRepo.Setup(x => x.GetAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>(), It.IsAny<SortDefinition<Data.Models.Task>?>(), null, null))
            .Callback<FilterDefinition<Data.Models.Task>, CancellationToken, SortDefinition<Data.Models.Task>?, int?, int?>((f, _, s, __, ___) =>
            {
                capturedFilter = f;
                capturedSort = s;
            })
            .ReturnsAsync(new OperationResult<IReadOnlyList<Data.Models.Task>>().WithRelatedObject(Array.Empty<Data.Models.Task>()));

        await sut.GetTasks(userId, null, CancellationToken.None);

        Assert.NotNull(capturedFilter);
        var renderedFilter = RenderFilter(capturedFilter!);
        Assert.Equal(userId, renderedFilter["UserId"].AsObjectId);

        Assert.NotNull(capturedSort);
        var renderedSort = RenderSort(capturedSort!);
        Assert.True(renderedSort.Contains("CreatedAt"));
        Assert.Equal(-1, renderedSort["CreatedAt"].AsInt32);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTasks_ShouldIncludeCategoryFilter_When_CategoryId_Provided()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var categoryId = ObjectId.GenerateNewId();

        FilterDefinition<Data.Models.Task>? capturedFilter = null;

        taskRepo.Setup(x => x.GetAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>(), It.IsAny<SortDefinition<Data.Models.Task>?>(), null, null))
            .Callback<FilterDefinition<Data.Models.Task>, CancellationToken, SortDefinition<Data.Models.Task>?, int?, int?>((f, _, __, ___, ____) => capturedFilter = f)
            .ReturnsAsync(new OperationResult<IReadOnlyList<Data.Models.Task>>().WithRelatedObject(Array.Empty<Data.Models.Task>()));

        await sut.GetTasks(userId, categoryId.ToString(), CancellationToken.None);

        Assert.NotNull(capturedFilter);
        var rendered = RenderFilter(capturedFilter!);

        if (rendered.Contains("$and"))
        {
            var andArray = rendered["$and"].AsBsonArray;
            Assert.Contains(andArray, x => x.AsBsonDocument.Contains("CategoryId") && x.AsBsonDocument["CategoryId"].AsObjectId == categoryId);
        }
        else
        {
            Assert.Equal(categoryId, rendered["CategoryId"].AsObjectId);
        }
    }

    #endregion

    #region DeleteOne Tests

    [Fact]
    public async System.Threading.Tasks.Task DeleteOne_ShouldReturnError_When_UserId_IsInvalid()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var result = await sut.DeleteOne(ObjectId.GenerateNewId(), ObjectId.Empty, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid user id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteOne_ShouldCallRepository_WithFilter_OnIdAndUserId()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var taskId = ObjectId.GenerateNewId();

        FilterDefinition<Data.Models.Task>? capturedFilter = null;

        taskRepo.Setup(x => x.DeleteOneAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<Data.Models.Task>, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync(new OperationResult<Data.Models.Task>());

        await sut.DeleteOne(taskId, userId, CancellationToken.None);

        Assert.NotNull(capturedFilter);
        var rendered = RenderFilter(capturedFilter!);

        if (rendered.Contains("$and"))
        {
            var andArray = rendered["$and"].AsBsonArray;
            Assert.Contains(andArray, x => x.AsBsonDocument.Contains("_id") && x.AsBsonDocument["_id"].AsObjectId == taskId);
            Assert.Contains(andArray, x => x.AsBsonDocument.Contains("UserId") && x.AsBsonDocument["UserId"].AsObjectId == userId);
        }
        else
        {
            Assert.Equal(taskId, rendered["_id"].AsObjectId);
            Assert.Equal(userId, rendered["UserId"].AsObjectId);
        }
    }

    #endregion

    #region DeleteMany Tests

    [Fact]
    public async System.Threading.Tasks.Task DeleteMany_WithTaskIds_ShouldReturnError_When_UserId_IsInvalid()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var result = await sut.DeleteMany(new[] { ObjectId.GenerateNewId() }, ObjectId.Empty, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid user id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteMany_WithTaskIds_ShouldReturnError_When_SomeTasks_NotFound()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var taskId1 = ObjectId.GenerateNewId();
        var taskId2 = ObjectId.GenerateNewId();
        var taskId3 = ObjectId.GenerateNewId();

        var foundTasks = new List<Data.Models.Task>
        {
            new Data.Models.Task { Id = taskId1, UserId = userId },
            new Data.Models.Task { Id = taskId2, UserId = userId }
        };

        taskRepo.Setup(x => x.GetAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>(), null, null, null))
            .ReturnsAsync(new OperationResult<IReadOnlyList<Data.Models.Task>>().WithRelatedObject(foundTasks));

        var result = await sut.DeleteMany(new[] { taskId1, taskId2, taskId3 }, userId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e is NotFoundError && e.ToString()!.Contains("Some tasks were not found"));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteMany_WithTaskIds_ShouldDelete_When_AllTasks_Found()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var taskId1 = ObjectId.GenerateNewId();
        var taskId2 = ObjectId.GenerateNewId();

        var foundTasks = new List<Data.Models.Task>
        {
            new Data.Models.Task { Id = taskId1, UserId = userId },
            new Data.Models.Task { Id = taskId2, UserId = userId }
        };

        taskRepo.Setup(x => x.GetAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>(), null, null, null))
            .ReturnsAsync(new OperationResult<IReadOnlyList<Data.Models.Task>>().WithRelatedObject(foundTasks));

        taskRepo.Setup(x => x.DeleteManyAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<long>().WithRelatedObject(2L));

        var result = await sut.DeleteMany(new[] { taskId1, taskId2 }, userId, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2L, result.ResultObject);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteMany_WithUserId_ShouldCallRepository_WithFilter_OnUserId()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();

        FilterDefinition<Data.Models.Task>? capturedFilter = null;

        taskRepo.Setup(x => x.DeleteManyAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<Data.Models.Task>, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync(new OperationResult<long>().WithRelatedObject(5L));

        await sut.DeleteMany(userId, CancellationToken.None);

        Assert.NotNull(capturedFilter);
        var rendered = RenderFilter(capturedFilter!);
        Assert.Equal(userId, rendered["UserId"].AsObjectId);
    }

    #endregion

    #region DeleteByCategoryId Tests

    [Fact]
    public async System.Threading.Tasks.Task DeleteByCategoryId_Single_ShouldReturnError_When_UserId_IsInvalid()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var result = await sut.DeleteByCategoryId(ObjectId.GenerateNewId(), ObjectId.Empty, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid user id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteByCategoryId_Single_ShouldReturnError_When_Category_DoesNotExist()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var categoryId = ObjectId.GenerateNewId();

        categoryRepo.Setup(x => x.AnyAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<bool>().WithRelatedObject(false));

        var result = await sut.DeleteByCategoryId(categoryId, userId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e is NotFoundError && e.ToString()!.Contains($"Category with id {categoryId} not found."));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteByCategoryId_Single_ShouldDeleteTasks_When_Category_Exists()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var categoryId = ObjectId.GenerateNewId();

        categoryRepo.Setup(x => x.AnyAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<bool>().WithRelatedObject(true));

        FilterDefinition<Data.Models.Task>? capturedFilter = null;

        taskRepo.Setup(x => x.DeleteManyAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<Data.Models.Task>, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync(new OperationResult<long>().WithRelatedObject(3L));

        var result = await sut.DeleteByCategoryId(categoryId, userId, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(3L, result.ResultObject);

        Assert.NotNull(capturedFilter);
        var rendered = RenderFilter(capturedFilter!);

        if (rendered.Contains("$and"))
        {
            var andArray = rendered["$and"].AsBsonArray;
            Assert.Contains(andArray, x => x.AsBsonDocument.Contains("UserId") && x.AsBsonDocument["UserId"].AsObjectId == userId);
            Assert.Contains(andArray, x => x.AsBsonDocument.Contains("CategoryId") && x.AsBsonDocument["CategoryId"].AsObjectId == categoryId);
        }
        else
        {
            Assert.Equal(userId, rendered["UserId"].AsObjectId);
            Assert.Equal(categoryId, rendered["CategoryId"].AsObjectId);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteByCategoryId_Multiple_ShouldReturnError_When_UserId_IsInvalid()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var result = await sut.DeleteByCategoryId(new[] { ObjectId.GenerateNewId() }, ObjectId.Empty, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid user id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteByCategoryId_Multiple_ShouldReturnError_When_NoCategories_Provided()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var result = await sut.DeleteByCategoryId(Array.Empty<ObjectId>(), ObjectId.GenerateNewId(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("No categories provided."));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteByCategoryId_Multiple_ShouldReturnError_When_CategoryId_IsEmpty()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var result = await sut.DeleteByCategoryId(new[] { ObjectId.GenerateNewId(), ObjectId.Empty }, ObjectId.GenerateNewId(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid category id."));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteByCategoryId_Multiple_ShouldReturnError_When_SomeCategories_NotFound()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var categoryId1 = ObjectId.GenerateNewId();
        var categoryId2 = ObjectId.GenerateNewId();
        var categoryId3 = ObjectId.GenerateNewId();

        var foundCategories = new List<Category>
        {
            new Category { Id = categoryId1, UserId = userId },
            new Category { Id = categoryId2, UserId = userId }
        };

        categoryRepo.Setup(x => x.GetAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>(), null, null, null))
            .ReturnsAsync(new OperationResult<IReadOnlyList<Category>>().WithRelatedObject(foundCategories));

        var result = await sut.DeleteByCategoryId(new[] { categoryId1, categoryId2, categoryId3 }, userId, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e is NotFoundError && e.ToString()!.Contains("Some categories were not found"));
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteByCategoryId_Multiple_ShouldDeleteTasks_When_AllCategories_Found()
    {
        var (sut, taskRepo, categoryRepo) = CreateSut();

        var userId = ObjectId.GenerateNewId();
        var categoryId1 = ObjectId.GenerateNewId();
        var categoryId2 = ObjectId.GenerateNewId();

        var foundCategories = new List<Category>
        {
            new Category { Id = categoryId1, UserId = userId },
            new Category { Id = categoryId2, UserId = userId }
        };

        categoryRepo.Setup(x => x.GetAsync(It.IsAny<FilterDefinition<Category>>(), It.IsAny<CancellationToken>(), null, null, null))
            .ReturnsAsync(new OperationResult<IReadOnlyList<Category>>().WithRelatedObject(foundCategories));

        FilterDefinition<Data.Models.Task>? capturedFilter = null;

        taskRepo.Setup(x => x.DeleteManyAsync(It.IsAny<FilterDefinition<Data.Models.Task>>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<Data.Models.Task>, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync(new OperationResult<long>().WithRelatedObject(10L));

        var result = await sut.DeleteByCategoryId(new[] { categoryId1, categoryId2 }, userId, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(10L, result.ResultObject);

        Assert.NotNull(capturedFilter);
        var rendered = RenderFilter(capturedFilter!);

        if (rendered.Contains("$and"))
        {
            var andArray = rendered["$and"].AsBsonArray;
            Assert.Contains(andArray, x => x.AsBsonDocument.Contains("UserId") && x.AsBsonDocument["UserId"].AsObjectId == userId);
            Assert.Contains(andArray, x => x.AsBsonDocument.Contains("CategoryId"));
        }
        else
        {
            Assert.Equal(userId, rendered["UserId"].AsObjectId);
            Assert.True(rendered.Contains("CategoryId"));
        }
    }

    #endregion
}