using FluentValidation;

namespace Api5.Application.Retros.Commands.RenameColumn;

/// <summary>
/// Validates <see cref="RenameColumnCommand"/> before the handler executes.
/// </summary>
public class RenameColumnCommandValidator : AbstractValidator<RenameColumnCommand>
{
    /// <summary>Initializes validation rules for column rename.</summary>
    public RenameColumnCommandValidator()
    {
        RuleFor(x => x.RetroBoardId).NotEmpty().WithMessage("RetroBoardId is required.");
        RuleFor(x => x.ColumnId).NotEmpty().WithMessage("ColumnId is required.");
        RuleFor(x => x.NewName)
            .NotEmpty().WithMessage("Column name is required.")
            .MaximumLength(200).WithMessage("Column name must not exceed 200 characters.");
    }
}
