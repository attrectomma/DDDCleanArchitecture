namespace Api1.Domain.Entities;

/// <summary>
/// Represents a retro column (e.g., "What went well", "What to improve").
/// A column belongs to a <see cref="RetroBoard"/> and contains <see cref="Note"/>s.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 the Column entity is a plain DTO with no behaviour.
/// Column name uniqueness within a retro is enforced in the ColumnService
/// via a check-then-act pattern — which is NOT safe under concurrency.
/// API 3 moves this enforcement into the RetroBoard aggregate root.
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
