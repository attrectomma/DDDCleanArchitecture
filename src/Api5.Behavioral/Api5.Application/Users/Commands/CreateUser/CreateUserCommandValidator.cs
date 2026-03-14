using FluentValidation;

namespace Api5.Application.Users.Commands.CreateUser;

/// <summary>
/// Validates <see cref="CreateUserCommand"/> before the handler executes.
/// </summary>
/// <remarks>
/// DESIGN: In API 4, validation was on the request DTO
/// (<c>CreateUserRequestValidator</c>) and triggered by ASP.NET model
/// binding via FluentValidation auto-validation. In API 5, validation
/// is on the COMMAND and triggered by the <c>ValidationBehavior</c>
/// pipeline. The handler never receives an invalid command.
///
/// This shift means validation is framework-agnostic — it works
/// regardless of whether the command comes from an HTTP controller,
/// a message queue, or a test.
/// </remarks>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    /// <summary>Initializes validation rules for user creation.</summary>
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(300).WithMessage("Email must not exceed 300 characters.");
    }
}
