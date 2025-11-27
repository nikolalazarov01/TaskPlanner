using System.Net;
using System.Net.Http.Json;
using TaskPlanner.API.Core.Models;
using Xunit;

namespace TestPlanner.Tests.Integration;

public class IdentityControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IdentityControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldReturnToken_ForNewUser()
    {
        var registerRequest = CreateRegisterRequest();

        var response = await _client.PostAsJsonAsync("/api/identity/register", registerRequest);
        var result = await response.Content.ReadFromJsonAsync<OperationResultDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result?.Success);
        Assert.False(string.IsNullOrEmpty(result?.ResultObject?.Token));
    }

    [Fact]
    public async Task Register_ShouldFail_WhenEmailAlreadyUsed()
    {
        var registerRequest = CreateRegisterRequest();

        var firstResponse = await _client.PostAsJsonAsync("/api/identity/register", registerRequest);
        firstResponse.EnsureSuccessStatusCode();

        var duplicateResponse = await _client.PostAsJsonAsync("/api/identity/register", registerRequest);
        var duplicateResult = await duplicateResponse.Content.ReadFromJsonAsync<OperationResultDto>();

        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        Assert.False(duplicateResult?.Success);
        Assert.NotEmpty(duplicateResult?.Errors ?? Array.Empty<OperationErrorDto>());
    }

    [Fact]
    public async Task Login_ShouldReturnToken_ForValidCredentials()
    {
        var registerRequest = CreateRegisterRequest();
        await _client.PostAsJsonAsync("/api/identity/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        var response = await _client.PostAsJsonAsync("/api/identity/login", loginRequest);
        var result = await response.Content.ReadFromJsonAsync<OperationResultDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result?.Success);
        Assert.False(string.IsNullOrEmpty(result?.ResultObject?.Token));
    }

    [Fact]
    public async Task Login_ShouldFail_ForUnknownEmail()
    {
        var loginRequest = new LoginRequest
        {
            Email = $"{Guid.NewGuid()}@example.com",
            Password = "Secret123!"
        };

        var response = await _client.PostAsJsonAsync("/api/identity/login", loginRequest);
        var result = await response.Content.ReadFromJsonAsync<OperationResultDto>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(result?.Success);
    }

    [Fact]
    public async Task Login_ShouldFail_ForInvalidPassword()
    {
        var registerRequest = CreateRegisterRequest();
        await _client.PostAsJsonAsync("/api/identity/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = "WrongPassword!"
        };

        var response = await _client.PostAsJsonAsync("/api/identity/login", loginRequest);
        var result = await response.Content.ReadFromJsonAsync<OperationResultDto>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(result?.Success);
    }

    private static RegisterRequest CreateRegisterRequest()
    {
        return new RegisterRequest
        {
            Email = $"{Guid.NewGuid()}@example.com",
            Password = "Secret123!",
            DisplayName = "Integration User"
        };
    }

    private sealed class OperationResultDto
    {
        public bool Success { get; set; }
        public AuthResponse? ResultObject { get; set; }
        public OperationErrorDto[]? Errors { get; set; }
    }

    private sealed class OperationErrorDto
    {
        public string? Message { get; set; }
    }
}

