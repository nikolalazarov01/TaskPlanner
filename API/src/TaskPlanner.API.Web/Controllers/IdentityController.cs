using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Identity;

namespace TaskPlanner.API.Web.Controllers;

/// <summary>
/// Exposes endpoints for user authentication and registration
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IdentityController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly IValidator<RegisterInputModel> registerRequestValidator;
    private readonly IValidator<LoginInputModel> loginRequestValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityController"/>
    /// </summary>
    /// <param name="identityService">Service that contains the business logic for registration and authentication</param>
    /// <param name="registerRequestValidator">Validator for <see cref="RegisterInputModel"/></param>
    /// <param name="loginRequestValidator">Validator for <see cref="LoginInputModel"/></param>
    public IdentityController(IIdentityService identityService, IValidator<RegisterInputModel> registerRequestValidator, IValidator<LoginInputModel> loginRequestValidator)
    {
        this._identityService = identityService;
        this.registerRequestValidator = registerRequestValidator;
        this.loginRequestValidator = loginRequestValidator;
    }

    /// <summary>
    /// Login - User
    /// </summary>
    /// <param name="inputModel">The input model containing the user's email and password</param>
    /// <returns>Returns an authentication response containing a token if the credentials are valid</returns>
    /// <remarks>
    /// Authenticates a user using email and password. If authentication succeeds, the response contains the generated token.
    /// </remarks>
    /// <response code="200">Returns an authentication response containing the token</response>
    /// <response code="400">The request body is missing required fields</response>
    /// <response code="401">The credentials are invalid or validation failed</response>
    /// <response code="404">The authentication succeeded but the response payload was not returned</response>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginInputModel inputModel)
    {
        if (inputModel is null || string.IsNullOrEmpty(inputModel.Email) || string.IsNullOrWhiteSpace(inputModel.Password))
            return BadRequest();
        
        var loginValidation = await this.loginRequestValidator.ValidateAsync(inputModel);
        if (!loginValidation.IsValid) return Unauthorized(loginValidation);
        
        var result = await _identityService.LoginAsync(inputModel);

        if (!result.Success) return Unauthorized(result);
        
        var loginResponse = result.ResultObject;
        if (loginResponse is null) return NotFound();

        return Ok(loginResponse);
    }

    /// <summary>
    /// Register - User
    /// </summary>
    /// <param name="inputModel">The input model containing the user's registration data</param>
    /// <returns>Returns OK if the user is registered successfully</returns>
    /// <remarks>
    /// Registers a new user account. Validation is performed before attempting to create the user.
    /// </remarks>
    /// <response code="200">The user was registered successfully</response>
    /// <response code="400">The request body is invalid, validation failed, or registration failed</response>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterInputModel inputModel)
    {
        //TO-DO: Add password strength validation and email validation
        if (inputModel is null || string.IsNullOrEmpty(inputModel.Email) || string.IsNullOrWhiteSpace(inputModel.Password) || string.IsNullOrWhiteSpace(inputModel.DisplayName))
            return BadRequest();
        
        var validationResult = await registerRequestValidator.ValidateAsync(inputModel);
        
        if (!validationResult.IsValid)
            return BadRequest(validationResult);
        
        var result = await _identityService.RegisterAsync(inputModel);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok();
    }
}
