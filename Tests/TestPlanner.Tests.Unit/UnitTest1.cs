/*using MongoDB.Bson;
using TaskPlanner.API.Core.Services;
using TaskEntity = TaskPlanner.API.Data.Models.Task;
using Xunit;

namespace TestPlanner.Tests.Unit;

public class BaseServiceTests
{
    private sealed class FakeTaskService : BaseService<TaskEntity>
    {
    }

    [Fact]
    public async Task CreateAsync_ShouldPersist_Task_InMemory()
    {
        var database = new InMemoryDatabase();
        var service = new FakeTaskService();
        var task = new TaskEntity
        {
            Id = ObjectId.GenerateNewId(),
            Description = "Write integration tests"
        };

        // expected future behavior once services persist entities
        var created = await service.CreateAsync(task);
        database.Save(task);

        Assert.Equal(task.Id, created.Id);
        Assert.NotNull(database.Get<TaskEntity>(task.Id));
    }

    [Fact]
    public async Task UpdateAsync_ShouldModify_Task_InMemory()
    {
        var database = new InMemoryDatabase();
        var service = new FakeTaskService();
        var task = new TaskEntity
        {
            Id = ObjectId.GenerateNewId(),
            Description = "Original"
        };

        database.Save(task);

        task.Description = "Updated description";

        var updated = await service.UpdateAsync(task);
        database.Save(task);

        Assert.Equal("Updated description", updated.Description);
        Assert.Equal("Updated description", database.Get<TaskEntity>(task.Id)?.Description);
    }
}*/