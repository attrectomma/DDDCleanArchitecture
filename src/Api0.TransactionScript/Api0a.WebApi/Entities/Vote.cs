namespace Api0a.WebApi.Entities;

/// <summary>
/// Represents a vote cast by a <see cref="User"/> on a <see cref="Note"/>.
/// </summary>
/// <remarks>
/// DESIGN: The one-vote-per-user-per-note invariant is enforced by the
/// endpoint handler using a check-then-act query. Under concurrent access,
/// two votes from the same user can both pass the check. A unique index
/// on (NoteId, UserId) acts as a safety net but the resulting DB exception
/// is not caught in Api0a (fixed in Api0b).
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
