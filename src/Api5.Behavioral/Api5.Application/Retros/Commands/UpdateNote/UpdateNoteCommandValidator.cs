using FluentValidation;

namespace Api5.Application.Retros.Commands.UpdateNote;

/// <summary>
/// Validates <see cref="UpdateNoteCommand"/> before the handler executes.
/// </summary>
public class UpdateNoteCommandValidator : AbstractValidator<UpdateNoteCommand>
{
    /// <summary>Initializes validation rules for note update.</summary>
    public UpdateNoteCommandValidator()
    {
        RuleFor(x => x.ColumnId).NotEmpty().WithMessage("ColumnId is required.");
        RuleFor(x => x.NoteId).NotEmpty().WithMessage("NoteId is required.");
        RuleFor(x => x.NewText)
            .NotEmpty().WithMessage("Note text is required.")
            .MaximumLength(2000).WithMessage("Note text must not exceed 2000 characters.");
    }
}
