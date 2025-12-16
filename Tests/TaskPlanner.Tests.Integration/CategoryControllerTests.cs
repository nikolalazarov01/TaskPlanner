using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using TaskPlanner.API.Core.Models.Category;
using TaskPlanner.API.Core.Services;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Data.Repositories;
using TaskPlanner.API.Web.Controllers;
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
        
        var controller = new CategoryController(service);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, ObjectId.GenerateNewId().ToString())
            // If your app uses "sub" instead, use: new Claim("sub", userId.ToString())
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

        var statusResult = Assert.IsType<OkResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusResult.StatusCode);
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

