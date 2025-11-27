using System.Net.Http.Headers;
using System.Net.Http.Json;
using MongoDB.Bson;
using Category = TaskPlanner.API.Data.Models.Category;
using Xunit;

namespace TestPlanner.Tests.Integration;

public class CategoryControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly InMemoryDatabase _database = new();

    public CategoryControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateToken());
    }

    [Fact]
    public async Task Post_ShouldPersist_Category_InMemory()
    {
        var category = new Category
        {
            Id = ObjectId.GenerateNewId(),
            Name = "Work"
        };

        var response = await _client.PostAsJsonAsync("/api/category", category);
        response.EnsureSuccessStatusCode();

        _database.Save(category);
        Assert.NotNull(_database.Get<Category>(category.Id));
    }

    [Fact]
    public async Task Get_ShouldReturn_Category_Data()
    {
        var category = new Category
        {
            Id = ObjectId.GenerateNewId(),
            Name = "Personal"
        };

        _database.Save(category);

        var response = await _client.GetAsync($"/api/category?entityId={category.Id}");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<Category>();
        Assert.Equal(category.Id, payload!.Id);
    }
}

