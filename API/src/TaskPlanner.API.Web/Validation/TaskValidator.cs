using FluentValidation;
using MongoDB.Bson;
using TaskPlanner.API.Core.Models.Task;

namespace TaskPlanner.API.Web.Validation;

public class TaskValidator : AbstractValidator<CreateTaskInputModel>
{
    public TaskValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Task description is required.")
            .MaximumLength(50)
            .WithMessage("Task description must not exceed 50 characters.");

        RuleFor(x => x.Deadline)
            .Must(d => d is null || d.Value > DateTime.UtcNow)
            .WithMessage("Deadline must be in the future.");

        RuleFor(x => x.EstimatedMinutes)
            .GreaterThanOrEqualTo(0)
            .When(x => x.EstimatedMinutes.HasValue)
            .WithMessage("Estimated minutes must be greater than or equal to 0.");
    }
}

public class UpdateTaskValidator : AbstractValidator<UpdateTaskInputModel>
{
    public UpdateTaskValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Task id is required.")
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Task id must be a valid ObjectId.");

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("Category id is required.")
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Category id must be a valid ObjectId.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Task description is required.")
            .MaximumLength(50)
            .WithMessage("Task description must not exceed 50 characters.");

        RuleFor(x => x.Deadline)
            .Must(d => d is null || d.Value > DateTime.UtcNow)
            .WithMessage("Deadline must be in the future.");

        RuleFor(x => x.EstimatedMinutes)
            .GreaterThanOrEqualTo(0)
            .When(x => x.EstimatedMinutes.HasValue)
            .WithMessage("Estimated minutes must be greater than or equal to 0.");
    }
}

public class TaskCategoryIdValidator : AbstractValidator<string>
{
    public TaskCategoryIdValidator()
    {
        RuleFor(x => x)
            .NotEmpty()
            .WithMessage("Category id is required.")
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Category id must be a valid ObjectId.");
    }
}
