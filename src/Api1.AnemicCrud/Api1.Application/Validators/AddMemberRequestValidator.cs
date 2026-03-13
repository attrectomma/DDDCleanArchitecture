using Api1.Application.DTOs.Requests;
using FluentValidation;

namespace Api1.Application.Validators;

/// <summary>
/// Validates <see cref="AddMemberRequest"/> input.
/// </summary>
public class AddMemberRequestValidator : AbstractValidator<AddMemberRequest>
{
    /// <summary>Initializes validation rules for adding a project member.</summary>
    public AddMemberRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
