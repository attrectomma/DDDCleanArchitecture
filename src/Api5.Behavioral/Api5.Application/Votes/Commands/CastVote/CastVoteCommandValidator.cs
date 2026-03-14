using FluentValidation;

namespace Api5.Application.Votes.Commands.CastVote;

/// <summary>
/// Validates <see cref="CastVoteCommand"/> before the handler executes.
/// </summary>
/// <remarks>
/// DESIGN: Validation is a pipeline behavior, not inline code.
/// The handler never receives an invalid command.
/// </remarks>
public class CastVoteCommandValidator : AbstractValidator<CastVoteCommand>
{
    /// <summary>Initializes validation rules for casting a vote.</summary>
    public CastVoteCommandValidator()
    {
        RuleFor(x => x.NoteId).NotEmpty().WithMessage("NoteId is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
    }
}
