using FluentValidation;
using TaskPlanner.API.Core.Models.Category;

namespace TaskPlanner.API.Web.Validation;

public class CategoryValidator : AbstractValidator<CategoryInputModel>
{
    public CategoryValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Category name must not exceed 100 characters.");
        
        RuleFor(c => c.Color)
            .Matches("^#(?:[0-9a-fA-F]{3}){1,2}$")
            .When(c => !string.IsNullOrWhiteSpace(c.Color))
            .WithMessage("Color must be a valid hex value (e.g. #FFF or #FFFFFF).");
    }
}