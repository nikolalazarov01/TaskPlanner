using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.API.Core.Interfaces;
using TaskPlanner.API.Core.Models.Identity;

namespace TaskPlanner.API.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IdentityController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly IValidator<RegisterInputModel> registerRequestValidator;
    private readonly IValidator<LoginInputModel> loginRequestValidator;

    public IdentityController(IIdentityService identityService, IValidator<RegisterInputModel> registerRequestValidator, IValidator<LoginInputModel> loginRequestValidator)
    {
        this._identityService = identityService;
        this.registerRequestValidator = registerRequestValidator;
        this.loginRequestValidator = loginRequestValidator;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginInputModel inputModel)
    {
        if (inputModel is null || string.IsNullOrEmpty(inputModel.Email) || string.IsNullOrWhiteSpace(inputModel.Password))
            return BadRequest();
        
        var result = await _identityService.LoginAsync(inputModel);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

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

