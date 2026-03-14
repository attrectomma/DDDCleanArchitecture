using Api3.Application.DTOs.Requests;
using FluentValidation;

namespace Api3.Application.Validators;

/// <summary>
/// Validates <see cref="CreateRetroBoardRequest"/> input.
/// </summary>
public class CreateRetroBoardRequestValidator : AbstractValidator<CreateRetroBoardRequest>
{
    /// <summary>Initializes validation rules for retro board creation.</summary>
    public CreateRetroBoardRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Retro board name is required.")
            .MaximumLength(300).WithMessage("Retro board name must not exceed 300 characters.");
    }
}
