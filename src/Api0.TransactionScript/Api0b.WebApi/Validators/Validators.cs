using Api0b.WebApi.DTOs;
using FluentValidation;

namespace Api0b.WebApi.Validators;

/// <summary>Validates <see cref="CreateUserRequest"/> input.</summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    /// <summary>Initializes validation rules for user creation.</summary>
    public CreateUserRequestValidator()
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

/// <summary>Validates <see cref="CreateProjectRequest"/> input.</summary>
public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    /// <summary>Initializes validation rules for project creation.</summary>
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(300).WithMessage("Project name must not exceed 300 characters.");
    }
}

/// <summary>Validates <see cref="AddMemberRequest"/> input.</summary>
public class AddMemberRequestValidator : AbstractValidator<AddMemberRequest>
{
    /// <summary>Initializes validation rules for adding a project member.</summary>
    public AddMemberRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}

/// <summary>Validates <see cref="CreateRetroBoardRequest"/> input.</summary>
public class CreateRetroBoardRequestValidator : AbstractValidator<CreateRetroBoardRequest>
{
    /// <summary>Initializes validation rules for retro board creation.</summary>
    public CreateRetroBoardRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Retro board name is required.")
            .MaximumLength(300).WithMessage("Retro board name must not exceed 300 characters.");
    }
}

/// <summary>Validates <see cref="CreateColumnRequest"/> input.</summary>
public class CreateColumnRequestValidator : AbstractValidator<CreateColumnRequest>
{
    /// <summary>Initializes validation rules for column creation.</summary>
    public CreateColumnRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Column name is required.")
            .MaximumLength(200).WithMessage("Column name must not exceed 200 characters.");
    }
}

/// <summary>Validates <see cref="UpdateColumnRequest"/> input.</summary>
public class UpdateColumnRequestValidator : AbstractValidator<UpdateColumnRequest>
{
    /// <summary>Initializes validation rules for column update.</summary>
    public UpdateColumnRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Column name is required.")
            .MaximumLength(200).WithMessage("Column name must not exceed 200 characters.");
    }
}

/// <summary>Validates <see cref="CreateNoteRequest"/> input.</summary>
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

/// <summary>Validates <see cref="UpdateNoteRequest"/> input.</summary>
public class UpdateNoteRequestValidator : AbstractValidator<UpdateNoteRequest>
{
    /// <summary>Initializes validation rules for note update.</summary>
    public UpdateNoteRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Note text is required.")
            .MaximumLength(2000).WithMessage("Note text must not exceed 2000 characters.");
    }
}

/// <summary>Validates <see cref="CastVoteRequest"/> input.</summary>
public class CastVoteRequestValidator : AbstractValidator<CastVoteRequest>
{
    /// <summary>Initializes validation rules for casting a vote.</summary>
    public CastVoteRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
