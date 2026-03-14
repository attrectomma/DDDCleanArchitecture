using FluentValidation;

namespace Api5.Application.Projects.Commands.AddMember;

/// <summary>
/// Validates <see cref="AddMemberCommand"/> before the handler executes.
/// </summary>
public class AddMemberCommandValidator : AbstractValidator<AddMemberCommand>
{
    /// <summary>Initializes validation rules for adding a project member.</summary>
    public AddMemberCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty().WithMessage("ProjectId is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
    }
}
