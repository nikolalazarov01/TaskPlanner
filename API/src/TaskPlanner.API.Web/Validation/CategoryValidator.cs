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

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryInputModel>
{
    public UpdateCategoryValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty()
            .WithMessage("Category id is required.");

        RuleFor(c => c.Name)
            .MaximumLength(100)
            .When(c => c.Name is not null)
            .WithMessage("Category name must not exceed 100 characters.");

        RuleFor(c => c.Color)
            .Matches("^#(?:[0-9a-fA-F]{3}){1,2}$")
            .When(c => c.Color is not null)
            .WithMessage("Color must be a valid hex value (e.g. #FFF or #FFFFFF).");
    }
}