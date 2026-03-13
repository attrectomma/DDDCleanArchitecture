using Api1.Application.DTOs.Requests;
using FluentValidation;

namespace Api1.Application.Validators;

/// <summary>
/// Validates <see cref="CreateNoteRequest"/> input.
/// </summary>
public class CreateNoteRequestValidator : AbstractValidator<CreateNoteRequest>
{
    /// <summary>Initializes validation rules for note creation.</summary>
    public CreateNoteRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Note text is required.")
            .MaximumLength(2000).WithMessage("Note text must not exceed 2000 characters.");
    }
}
