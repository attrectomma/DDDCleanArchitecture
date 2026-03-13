using Api1.Application.DTOs.Requests;
using FluentValidation;

namespace Api1.Application.Validators;

/// <summary>
/// Validates <see cref="CreateColumnRequest"/> input.
/// </summary>
public class CreateColumnRequestValidator : AbstractValidator<CreateColumnRequest>
{
    /// <summary>Initializes validation rules for column creation.</summary>
    public CreateColumnRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Column name is required.")
            .MaximumLength(200).WithMessage("Column name must not exceed 200 characters.");
    }
}
