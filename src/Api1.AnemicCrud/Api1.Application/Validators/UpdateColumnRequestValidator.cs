using Api1.Application.DTOs.Requests;
using FluentValidation;

namespace Api1.Application.Validators;

/// <summary>
/// Validates <see cref="UpdateColumnRequest"/> input.
/// </summary>
public class UpdateColumnRequestValidator : AbstractValidator<UpdateColumnRequest>
{
    /// <summary>Initializes validation rules for column update.</summary>
    public UpdateColumnRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Column name is required.")
            .MaximumLength(200).WithMessage("Column name must not exceed 200 characters.");
    }
}
