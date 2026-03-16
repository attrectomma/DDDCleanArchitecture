namespace Api0b.WebApi.Entities;

/// <summary>
/// Represents a note (sticky note) within a <see cref="Column"/>.
/// A note can receive <see cref="Vote"/>s from users.
/// </summary>
/// <remarks>
/// DESIGN: Note text uniqueness within a column is enforced by the endpoint
/// handler AND by a DB unique constraint. In Api0b, the middleware catches
/// constraint violations so concurrent duplicates get a proper 409.
/// </remarks>
public class Note : AuditableEntityBase
{
    /// <summary>Gets or sets the ID of the column this note belongs to.</summary>
    public Guid ColumnId { get; set; }

    /// <summary>Gets or sets the text content of the note.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the navigation property to the owning column.</summary>
    public Column Column { get; set; } = null!;

    /// <summary>Gets or sets the collection of votes on this note.</summary>
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
