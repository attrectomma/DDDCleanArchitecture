using FluentValidation;

namespace Api5.Application.Retros.Commands.AddNote;

/// <summary>
/// Validates <see cref="AddNoteCommand"/> before the handler executes.
/// </summary>
public class AddNoteCommandValidator : AbstractValidator<AddNoteCommand>
{
    /// <summary>Initializes validation rules for note creation.</summary>
    public AddNoteCommandValidator()
    {
        RuleFor(x => x.ColumnId).NotEmpty().WithMessage("ColumnId is required.");
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Note text is required.")
            .MaximumLength(2000).WithMessage("Note text must not exceed 2000 characters.");
    }
}
