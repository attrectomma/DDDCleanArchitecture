using Api3.Application.DTOs.Requests;
using FluentValidation;

namespace Api3.Application.Validators;

/// <summary>
/// Validates <see cref="CreateProjectRequest"/> input.
/// </summary>
public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    /// <summary>Initializes validation rules for project creation.</summary>
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(300).WithMessage("Project name must not exceed 300 characters.");
    }
}
