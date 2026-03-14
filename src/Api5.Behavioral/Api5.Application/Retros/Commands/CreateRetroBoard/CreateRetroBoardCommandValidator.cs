using FluentValidation;

namespace Api5.Application.Retros.Commands.CreateRetroBoard;

/// <summary>
/// Validates <see cref="CreateRetroBoardCommand"/> before the handler executes.
/// </summary>
public class CreateRetroBoardCommandValidator : AbstractValidator<CreateRetroBoardCommand>
{
    /// <summary>Initializes validation rules for retro board creation.</summary>
    public CreateRetroBoardCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("ProjectId is required.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Retro board name is required.")
            .MaximumLength(300).WithMessage("Retro board name must not exceed 300 characters.");
    }
}
