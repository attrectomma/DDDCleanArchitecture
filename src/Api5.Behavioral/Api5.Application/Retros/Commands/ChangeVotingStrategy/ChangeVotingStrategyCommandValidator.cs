using FluentValidation;

namespace Api5.Application.Retros.Commands.ChangeVotingStrategy;

/// <summary>
/// Validates <see cref="ChangeVotingStrategyCommand"/> before the handler executes.
/// </summary>
/// <remarks>
/// DESIGN: Validates that the retro board ID is provided and the strategy type
/// is a recognised enum value. This prevents invalid enum values from reaching
/// the domain layer.
/// </remarks>
public class ChangeVotingStrategyCommandValidator : AbstractValidator<ChangeVotingStrategyCommand>
{
    /// <summary>Initializes validation rules for changing the voting strategy.</summary>
    public ChangeVotingStrategyCommandValidator()
    {
        RuleFor(x => x.RetroBoardId)
            .NotEmpty()
            .WithMessage("RetroBoardId is required.");

        RuleFor(x => x.VotingStrategyType)
            .IsInEnum()
            .WithMessage("VotingStrategyType must be a valid strategy (Default or Budget).");
    }
}
