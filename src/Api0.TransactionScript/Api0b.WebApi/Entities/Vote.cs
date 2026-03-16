namespace Api0b.WebApi.Entities;

/// <summary>
/// Represents a vote cast by a <see cref="User"/> on a <see cref="Note"/>.
/// </summary>
/// <remarks>
/// DESIGN: The one-vote-per-user-per-note invariant is enforced by the
/// endpoint handler using a check-then-act query AND by a DB unique index
/// on (NoteId, UserId). In Api0b, the middleware catches the
/// <c>DbUpdateException</c> from the constraint violation so concurrent
/// duplicate votes get a proper 409 instead of a 500.
/// </remarks>
public class Vote : AuditableEntityBase
{
    /// <summary>Gets or sets the ID of the note this vote is for.</summary>
    public Guid NoteId { get; set; }

    /// <summary>Gets or sets the ID of the user who cast this vote.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the navigation property to the note.</summary>
    public Note Note { get; set; } = null!;

    /// <summary>Gets or sets the navigation property to the user.</summary>
    public User User { get; set; } = null!;
}
