namespace Api1.Domain.Entities;

/// <summary>
/// Represents a vote cast by a <see cref="User"/> on a <see cref="Note"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 the one-vote-per-user-per-note invariant is enforced
/// by the VoteService using a check-then-act query. Under concurrent access,
/// two votes from the same user can both pass the check and create duplicates.
/// A unique index on (NoteId, UserId) in the database acts as a safety net,
/// but the service layer does not gracefully handle the resulting DB exception.
/// API 3+ enforce this within the aggregate boundary with proper concurrency control.
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
