namespace Api0b.WebApi.Entities;

/// <summary>
/// Represents a retro column (e.g., "What went well", "What to improve").
/// A column belongs to a <see cref="RetroBoard"/> and contains <see cref="Note"/>s.
/// </summary>
/// <remarks>
/// DESIGN: Column name uniqueness within a retro is enforced by the endpoint
/// handler via a check-then-act query AND by a DB unique constraint. In Api0b,
/// the middleware catches the <c>DbUpdateException</c> from the constraint
/// violation, so even if the race condition bypasses the application check,
/// the second request gets a proper 409 instead of a 500.
/// </remarks>
public class Column : AuditableEntityBase
{
    /// <summary>Gets or sets the ID of the retro board this column belongs to.</summary>
    public Guid RetroBoardId { get; set; }

    /// <summary>Gets or sets the name of the column.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the navigation property to the owning retro board.</summary>
    public RetroBoard RetroBoard { get; set; } = null!;

    /// <summary>Gets or sets the collection of notes in this column.</summary>
    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
