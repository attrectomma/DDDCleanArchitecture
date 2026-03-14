using FluentValidation;

namespace Api5.Application.Projects.Commands.CreateProject;

/// <summary>
/// Validates <see cref="CreateProjectCommand"/> before the handler executes.
/// </summary>
public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    /// <summary>Initializes validation rules for project creation.</summary>
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(300).WithMessage("Project name must not exceed 300 characters.");
    }
}
