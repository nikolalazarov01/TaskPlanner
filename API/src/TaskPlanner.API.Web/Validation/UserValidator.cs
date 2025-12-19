using FluentValidation;
using TaskPlanner.API.Core.Models;
using TaskPlanner.API.Core.Models.Identity;

namespace TaskPlanner.API.Web.Validation;

public class UserLoginValidator : AbstractValidator<LoginInputModel>
{
    public UserLoginValidator()
    {
        RuleFor(u => u.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Invalid email address.");

        RuleFor(u => u.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public class UserRegisterValidator : AbstractValidator<RegisterInputModel>
{
    public UserRegisterValidator()
    {
        RuleFor(u => u.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Invalid email address.");

        RuleFor(u => u.DisplayName)
            .NotEmpty()
            .MinimumLength(2)
            .WithMessage("Display name must be at least 2 characters.");

        RuleFor(u => u.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}