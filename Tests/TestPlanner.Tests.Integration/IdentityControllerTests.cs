using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models;
using TaskPlanner.API.Core.Services;
using TaskPlanner.API.Web.Controllers;
using Xunit;

namespace TestPlanner.Tests.Integration;

public class IdentityControllerTests
{
    private IdentityController CreateController(out InMemoryUserRepository repository)
    {
        repository = new InMemoryUserRepository();
        var jwtOptions = Options.Create(new JwtOptions
        {
            Secret = "replace-with-strong-secret",
            Issuer = "TaskPlanner",
            Audience = "TaskPlannerClients",
            ExpirationMinutes = 60
        });

        IIdentityService identityService = new IdentityService(repository, jwtOptions);
        return new IdentityController(identityService);
    }

    [Fact]
    public async Task Register_ShouldReturnToken_ForNewUser()
    {
        var controller = CreateController(out _);
        var registerRequest = CreateRegisterRequest();

        var actionResult = await controller.Register(registerRequest);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var payload = Assert.IsType<OperationResult<AuthResponse>>(okResult.Value);
        Assert.True(payload.Success);
        Assert.False(string.IsNullOrEmpty(payload.ResultObject?.Token));
    }

    [Fact]
    public async Task Register_ShouldFail_WhenEmailAlreadyUsed()
    {
        var controller = CreateController(out _);
        var registerRequest = CreateRegisterRequest();

        await controller.Register(registerRequest);
        var duplicateResult = await controller.Register(registerRequest);

        var badRequest = Assert.IsType<BadRequestObjectResult>(duplicateResult);
        var payload = Assert.IsType<OperationResult<AuthResponse>>(badRequest.Value);
        Assert.False(payload.Success);
    }

    [Fact]
    public async Task Login_ShouldReturnToken_ForValidCredentials()
    {
        var controller = CreateController(out _);
        var registerRequest = CreateRegisterRequest();
        await controller.Register(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        var actionResult = await controller.Login(loginRequest);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var payload = Assert.IsType<OperationResult<AuthResponse>>(okResult.Value);
        Assert.True(payload.Success);
        Assert.False(string.IsNullOrEmpty(payload.ResultObject?.Token));
    }

    [Fact]
    public async Task Login_ShouldFail_ForUnknownEmail()
    {
        var controller = CreateController(out _);
        var loginRequest = new LoginRequest
        {
            Email = $"{Guid.NewGuid()}@example.com",
            Password = "Secret123!"
        };

        var actionResult = await controller.Login(loginRequest);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        var payload = Assert.IsType<OperationResult<AuthResponse>>(unauthorized.Value);
        Assert.False(payload.Success);
    }

    [Fact]
    public async Task Login_ShouldFail_ForInvalidPassword()
    {
        var controller = CreateController(out _);
        var registerRequest = CreateRegisterRequest();
        await controller.Register(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = "WrongPassword!"
        };

        var actionResult = await controller.Login(loginRequest);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        var payload = Assert.IsType<OperationResult<AuthResponse>>(unauthorized.Value);
        Assert.False(payload.Success);
    }

    private static RegisterRequest CreateRegisterRequest()
    {
        return new RegisterRequest
        {
            Email = $"{Guid.NewGuid()}@example.com",
            Password = "Secret123!",
            DisplayName = "Controller Test User"
        };
    }
}

