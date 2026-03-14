using Api3.Application.DTOs.Requests;
using FluentValidation;

namespace Api3.Application.Validators;

/// <summary>
/// Validates <see cref="CastVoteRequest"/> input.
/// </summary>
public class CastVoteRequestValidator : AbstractValidator<CastVoteRequest>
{
    /// <summary>Initializes validation rules for casting a vote.</summary>
    public CastVoteRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
