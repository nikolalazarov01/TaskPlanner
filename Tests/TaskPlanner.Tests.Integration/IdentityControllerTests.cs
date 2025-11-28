using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models;
using TaskPlanner.API.Core.Services;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Web.Controllers;

namespace TaskPlanner.Tests.Integration;

public class IdentityControllerTests : IClassFixture<Mongo2GoFixture>
{
    private readonly Mongo2GoFixture _mongoFixture;

    public IdentityControllerTests(Mongo2GoFixture mongoFixture)
    {
        _mongoFixture = mongoFixture;
    }
    
    private IdentityController CreateController()
    {
        IUserRepository repository = new MongoUserRepository(_mongoFixture);
        var jwtOptions = Options.Create(new JwtOptions
        {
            Secret = "this_is_a_very_long_test_secret_key_123!",
            Issuer = "TaskPlanner",
            Audience = "TaskPlannerClients",
            ExpirationMinutes = 60
        });

        IIdentityService identityService = new IdentityService(repository, jwtOptions);
        return new IdentityController(identityService);
    }
    
    private static RegisterInputModel CreateRegisterRequest()
    {
        return new RegisterInputModel
        {
            Email = $"{Guid.NewGuid()}@example.com",
            Password = "Secret123!",
            DisplayName = "Controller Test User"
        };
    }
    
    [Fact]
    public async Task Register_ShouldFail_WhenEmailAlreadyUsed()
    {
        var controller = CreateController();
        var registerRequest = CreateRegisterRequest();

        await controller.Register(registerRequest);
        var duplicateResult = await controller.Register(registerRequest);

        var badRequest = Assert.IsType<BadRequestObjectResult>(duplicateResult);
        var payload = Assert.IsType<OperationResult>(badRequest.Value);
        Assert.False(payload.Success);
    }
    
    [Fact]
    public async Task Register_ShouldFail_WhenPasswordIsWeak()
    {
        var controller = CreateController();
        var registerRequest = new RegisterInputModel
        {
            Email = $"{Guid.NewGuid()}@example.com",
            Password = "weak-password",
            DisplayName = "Controller Test User"
        };
        
        var registerResult = await controller.Register(registerRequest);

        var badRequest = Assert.IsType<BadRequestObjectResult>(registerResult);
        var payload = Assert.IsType<OperationResult>(badRequest.Value);
        Assert.False(payload.Success);
    }
    
    [Fact]
    public async Task Register_ShouldFail_WhenEmailIsNotCorrect()
    {
        var controller = CreateController();
        var registerRequest = new RegisterInputModel
        {
            Email = $"{Guid.NewGuid()}example.com",
            Password = "weak-password",
            DisplayName = "Controller Test User"
        };
        
        var registerResult = await controller.Register(registerRequest);

        var badRequest = Assert.IsType<BadRequestObjectResult>(registerResult);
        var payload = Assert.IsType<OperationResult>(badRequest.Value);
        Assert.False(payload.Success);
    }
    
    [Fact]
    public async Task Login_ShouldReturnToken_ForValidCredentials()
    {
        var controller = CreateController();
        var registerRequest = CreateRegisterRequest();
        await controller.Register(registerRequest);

        var loginRequest = new LoginInputModel
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
        var controller = CreateController();
        var loginRequest = new LoginInputModel
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
        var controller = CreateController();
        var registerRequest = CreateRegisterRequest();
        await controller.Register(registerRequest);

        var loginRequest = new LoginInputModel
        {
            Email = registerRequest.Email,
            Password = "WrongPassword!"
        };

        var actionResult = await controller.Login(loginRequest);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        var payload = Assert.IsType<OperationResult<AuthResponse>>(unauthorized.Value);
        Assert.False(payload.Success);
    }
    
    [Fact]
    public async Task Login_ShouldFail_ForEmptyValues()
    {
        var controller = CreateController();
        var registerRequest = CreateRegisterRequest();
        await controller.Register(registerRequest);

        var loginRequest = new LoginInputModel
        {
            Email = string.Empty,
            Password = string.Empty
        };

        var actionResult = await controller.Login(loginRequest);

        Assert.IsType<BadRequestResult>(actionResult);
    }
    
    [Fact]
    public async Task Login_ShouldFail_ForNullValues()
    {
        var controller = CreateController();
        var registerRequest = CreateRegisterRequest();
        await controller.Register(registerRequest);

        var loginRequest = new LoginInputModel
        {
            Email = null,
            Password = null
        };

        var actionResult = await controller.Login(loginRequest);

        Assert.IsType<BadRequestResult>(actionResult);
    }
    
    [Fact]
    public async Task Login_ShouldFail_ForNullRequestInputModel()
    {
        var controller = CreateController();
        var registerRequest = CreateRegisterRequest();
        await controller.Register(registerRequest);

        var actionResult = await controller.Login(null);

        Assert.IsType<BadRequestResult>(actionResult);
    }
}