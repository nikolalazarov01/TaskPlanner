using System.Net.Http.Headers;
using System.Net.Http.Json;
using MongoDB.Bson;
using TaskEntity = TaskPlanner.API.Data.Models.Task;
using Xunit;

namespace TestPlanner.Tests.Integration;

public class TaskControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly InMemoryDatabase _database = new();

    public TaskControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateToken());
    }

    [Fact]
    public async Task Post_ShouldPersist_Task_InMemory()
    {
        var task = new TaskEntity
        {
            Id = ObjectId.GenerateNewId(),
            Description = "Draft task in integration test"
        };

        var response = await _client.PostAsJsonAsync("/api/task", task);
        response.EnsureSuccessStatusCode();

        _database.Save(task);
        Assert.NotNull(_database.Get<TaskEntity>(task.Id));
    }

    [Fact]
    public async Task Delete_ShouldRemove_Task_FromMemory()
    {
        var task = new TaskEntity
        {
            Id = ObjectId.GenerateNewId(),
            Description = "Temporary"
        };

        _database.Save(task);

        var response = await _client.DeleteAsync($"/api/task?entityId={task.Id}");
        response.EnsureSuccessStatusCode();

        Assert.Null(_database.Get<TaskEntity>(task.Id));
    }
}

