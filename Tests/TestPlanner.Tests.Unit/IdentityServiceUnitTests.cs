using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using OneBitSoftware.Utilities;
using TaskPlanner.API.Core.Models.Identity;
using TaskPlanner.API.Core.Services;
using TaskPlanner.API.Data.Interfaces;
using TaskPlanner.API.Data.Models;
using TaskPlanner.API.Utilities;
using Task = System.Threading.Tasks.Task;

public class IdentityServiceTests
{
    private static (IdentityService Service, Mock<IBaseRepository<User>> Repo) CreateSut()
    {
        var repo = new Mock<IBaseRepository<User>>(MockBehavior.Strict);
        var jwtOptions = Options.Create(new JwtOptions
        {
            Secret = "THIS_IS_A_TEST_SECRET_THAT_IS_LONG_ENOUGH_32+",
            Issuer = "TaskPlanner.Tests",
            Audience = "TaskPlanner.Tests.Client",
            ExpirationMinutes = 60
        });

        var sut = new IdentityService(repo.Object, jwtOptions);
        return (sut, repo);
    }

    // REMOVE these filter rendering tests - they test MongoDB internals, not your service behavior
    // DELETE: RegisterAsync_ShouldCallGetOneAsync_WithEmailFilter
    // DELETE: LoginAsync_ShouldCallGetOneAsync_WithEmailFilter

    [Fact]
    public async Task RegisterAsync_ShouldReturnError_When_UserAlreadyExists()
    {
        var (sut, repo) = CreateSut();

        // User found = Success true with object
        repo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<User>().WithRelatedObject(new User()));

        var result = await sut.RegisterAsync(new RegisterInputModel
        {
            Email = "existing@test.com",
            Password = "pass",
            DisplayName = "User"
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("User already exists."));
        repo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_When_UserDoesNotExist()
    {
        var (sut, repo) = CreateSut();

        // User not found (based on your GetOneAsync implementation)
        var notFoundResult = new OperationResult<User>();
        notFoundResult.AppendError(new NotFoundError("Entity not found."));
        
        repo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notFoundResult);

        User? createdUser = null;
        repo.Setup(x => x.CreateAsync(It.IsAny<User>()))
            .Callback<User>(u => createdUser = u)
            .ReturnsAsync((User u) => new OperationResult<User>().WithRelatedObject(u));

        var input = new RegisterInputModel
        {
            Email = "new@user.com",
            Password = "P@ssw0rd",
            DisplayName = "New User"
        };

        var result = await sut.RegisterAsync(input);

        Assert.True(result.Success);
        repo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);
        
        Assert.NotNull(createdUser);
        Assert.Equal("new@user.com", createdUser!.Email);
        Assert.Equal("New User", createdUser.DisplayName);
        Assert.Equal("local", createdUser.AuthProvider);
        Assert.NotEqual(input.Password, createdUser.PasswordHash); // Verify hashed
        Assert.NotEmpty(createdUser.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_ShouldPropagateError_When_RepositoryCreateFails()
    {
        var (sut, repo) = CreateSut();

        var notFoundResult = new OperationResult<User>();
        notFoundResult.AppendError(new NotFoundError("not found"));

        repo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notFoundResult);

        var createError = new OperationResult<User>();
        createError.AppendError("Database connection failed");
        
        repo.Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(createError);

        var result = await sut.RegisterAsync(new RegisterInputModel
        {
            Email = "new@user.com",
            Password = "pass",
            DisplayName = "New"
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Database connection failed"));
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnError_When_UserNotFound()
    {
        var (sut, repo) = CreateSut();

        var notFoundResult = new OperationResult<User>();
        notFoundResult.AppendError(new NotFoundError("Entity not found."));
        
        repo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notFoundResult);

        var result = await sut.LoginAsync(new LoginInputModel
        {
            Email = "notfound@test.com",
            Password = "pass"
        });

        Assert.False(result.Success);
        // Your service returns repository errors directly, so check for those
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnError_When_PasswordIsIncorrect()
    {
        var (sut, repo) = CreateSut();

        var correctPassword = "CorrectPassword123";
        var storedHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(correctPassword)));

        var user = new User
        {
            Id = ObjectId.GenerateNewId(),
            Email = "user@test.com",
            DisplayName = "Test User",
            PasswordHash = storedHash
        };

        repo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<User>().WithRelatedObject(user));

        var result = await sut.LoginAsync(new LoginInputModel
        {
            Email = "user@test.com",
            Password = "WrongPassword"
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Invalid credentials"));
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_When_CredentialsAreValid()
    {
        var (sut, repo) = CreateSut();

        var password = "TestPassword123";
        var storedHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes(password)));

        var userId = ObjectId.GenerateNewId();
        var user = new User
        {
            Id = userId,
            Email = "user@test.com",
            DisplayName = "Test User",
            PasswordHash = storedHash,
            LastLoginAt = null
        };

        repo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<User>().WithRelatedObject(user));

        var result = await sut.LoginAsync(new LoginInputModel
        {
            Email = "user@test.com",
            Password = password
        });

        Assert.True(result.Success);
        Assert.NotNull(result.ResultObject);
        Assert.True(result.ResultObject!.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.ResultObject.Token));

        // Validate JWT token claims
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.ResultObject.Token);

        Assert.Equal(userId.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("user@test.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Test User", jwt.Claims.First(c => c.Type == "displayName").Value);
        
        // Verify LastLoginAt was updated (but note: not persisted!)
        Assert.NotNull(user.LastLoginAt);
    }

    [Fact]
    public async Task LoginAsync_ShouldHandleNullUser_Gracefully()
    {
        var (sut, repo) = CreateSut();

        // Edge case: Success true but null object (shouldn't happen with your repo, but good to handle)
        repo.Setup(x => x.GetOneAsync(It.IsAny<FilterDefinition<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperationResult<User>().WithRelatedObject(null));

        var result = await sut.LoginAsync(new LoginInputModel
        {
            Email = "test@test.com",
            Password = "pass"
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ToString()!.Contains("Something went wrong"));
    }
}