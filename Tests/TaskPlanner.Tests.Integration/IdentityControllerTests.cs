using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models;
using TaskPlanner.API.Core.Models.Identity;
using TaskPlanner.API.Core.Services;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Data.Repositories;
using TaskPlanner.API.Web.Controllers;
using TaskPlanner.API.Web.Validation;
using Task = System.Threading.Tasks.Task;

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
        var repository = new MongoDbRepositoryBase<User>(_mongoFixture.Database, "users");

        var jwtOptions = Options.Create(new JwtOptions
        {
            Secret = "this_is_a_very_long_test_secret_key_123!",
            Issuer = "TaskPlanner",
            Audience = "TaskPlannerClients",
            ExpirationMinutes = 60
        });

        IIdentityService identityService = new IdentityService(repository, jwtOptions);

        IValidator<RegisterInputModel> registerValidator = new UserRegisterValidator();
        IValidator<LoginInputModel> loginValidator = new UserLoginValidator();

        return new IdentityController(identityService, registerValidator, loginValidator);
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
        var validationResult = Assert.IsType<ValidationResult>(badRequest.Value);
        Assert.False(validationResult.IsValid);
        Assert.Contains(validationResult.Errors, e => e.PropertyName == "Password");
    }
    
    [Fact]
    public async Task Register_ShouldFail_WhenEmailIsNotCorrect()
    {
        // login validator can be the default "always valid"
        var controller = CreateController();
        
        var registerRequest = new RegisterInputModel
        {
            Email = $"{Guid.NewGuid()}example.com",
            Password = "StrongPassword1!",
            DisplayName = "Controller Test User"
        };
        
        var registerResult = await controller.Register(registerRequest);

        var badRequest = Assert.IsType<BadRequestObjectResult>(registerResult);
        var validationResult = Assert.IsType<ValidationResult>(badRequest.Value);
        Assert.False(validationResult.IsValid);
        Assert.Contains(validationResult.Errors, e => e.PropertyName == "Email");
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
        var payload = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.True(payload.Success);
        Assert.False(string.IsNullOrEmpty(payload.Token));
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
        var payload = Assert.IsType<ValidationResult>(unauthorized.Value);
        Assert.False(payload.IsValid);
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
    
    private static Mock<IValidator<RegisterInputModel>> GetValidRegisterValidatorMock()
    {
        var mock = new Mock<IValidator<RegisterInputModel>>();

        mock.Setup(v => v.ValidateAsync(
                It.IsAny<RegisterInputModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // no errors => IsValid == true

        return mock;
    }

    private static Mock<IValidator<LoginInputModel>> GetValidLoginValidatorMock()
    {
        var mock = new Mock<IValidator<LoginInputModel>>();

        mock.Setup(v => v.ValidateAsync(
                It.IsAny<LoginInputModel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // no errors

        return mock;
    }

}