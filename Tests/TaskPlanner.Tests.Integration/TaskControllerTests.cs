using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskEntity = TaskPlanner.API.Data.Models.Task;
using TaskPlanner.API.Web.Controllers;
using Xunit;

namespace TaskPlanner.Tests.Integration;

public class TaskControllerTests
{
    private readonly TaskController _controller = new();

    [Fact]
    public void Post_ShouldReturn_NotImplemented()
    {
        var task = new TaskEntity { Description = "Draft task" };

        var result = _controller.Post(task);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status501NotImplemented, statusResult.StatusCode);
    }

    [Fact]
    public void Put_ShouldReturn_NotImplemented()
    {
        var task = new TaskEntity { Description = "Update me" };

        var result = _controller.Put(task);

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

