using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Web.Controllers;
using Xunit;

namespace TestPlanner.Tests.Integration;

public class CategoryControllerTests
{
    private readonly CategoryController _controller = new();

    [Fact]
    public void Post_ShouldReturn_NotImplemented()
    {
        var category = new Category { Name = "Work" };

        var result = _controller.Post(category);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status501NotImplemented, statusResult.StatusCode);
    }

    [Fact]
    public void Put_ShouldReturn_NotImplemented()
    {
        var category = new Category { Name = "Personal" };

        var result = _controller.Put(category);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status501NotImplemented, statusResult.StatusCode);
    }

    [Fact]
    public void Get_ShouldReturn_NotImplemented()
    {
        var result = _controller.Get("dummy-id");

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status501NotImplemented, statusResult.StatusCode);
    }

    [Fact]
    public void Delete_ShouldReturn_NotImplemented()
    {
        var result = _controller.Delete("dummy-id");

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status501NotImplemented, statusResult.StatusCode);
    }
}

