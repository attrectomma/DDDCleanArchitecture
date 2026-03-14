using FluentValidation;

namespace Api5.Application.Retros.Commands.AddColumn;

/// <summary>
/// Validates <see cref="AddColumnCommand"/> before the handler executes.
/// </summary>
public class AddColumnCommandValidator : AbstractValidator<AddColumnCommand>
{
    /// <summary>Initializes validation rules for column creation.</summary>
    public AddColumnCommandValidator()
    {
        RuleFor(x => x.RetroBoardId).NotEmpty().WithMessage("RetroBoardId is required.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Column name is required.")
            .MaximumLength(200).WithMessage("Column name must not exceed 200 characters.");
    }
}
