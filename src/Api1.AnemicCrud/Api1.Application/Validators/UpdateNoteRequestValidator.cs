using Api1.Application.DTOs.Requests;
using FluentValidation;

namespace Api1.Application.Validators;

/// <summary>
/// Validates <see cref="UpdateNoteRequest"/> input.
/// </summary>
public class UpdateNoteRequestValidator : AbstractValidator<UpdateNoteRequest>
{
    /// <summary>Initializes validation rules for note update.</summary>
    public UpdateNoteRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Note text is required.")
            .MaximumLength(2000).WithMessage("Note text must not exceed 2000 characters.");
    }
}
