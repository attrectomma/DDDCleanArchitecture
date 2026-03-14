namespace Api2.Domain.Entities;

/// <summary>
/// Represents a retrospective board belonging to a <see cref="Project"/>.
/// A retro board contains multiple <see cref="Column"/>s.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, RetroBoard gets a factory constructor and an
/// <see cref="AddColumn"/> convenience method that checks column name
/// uniqueness. However, the <see cref="AddColumn"/> method requires the
/// entity to be loaded with its <see cref="Columns"/> collection — a
/// hidden coupling between loading strategy and domain logic.
///
/// In practice, the ColumnService in API 2 still uses a repository query
/// (<c>ExistsByNameInRetroAsync</c>) for the uniqueness check and creates
/// columns directly via the <see cref="Column"/> constructor. This is
/// because loading all columns just to add one is wasteful without
/// aggregate boundaries.
///
/// API 3 makes RetroBoard a true aggregate root that is always loaded
/// with its children, making <see cref="AddColumn"/> the primary way
/// to create columns with proper invariant enforcement.
/// </remarks>
public class RetroBoard : AuditableEntityBase
{
    private readonly List<Column> _columns = new();

    /// <summary>
    /// Required by EF Core for entity materialisation.
    /// </summary>
    private RetroBoard() { }

    /// <summary>
    /// Creates a new retro board for the specified project.
    /// </summary>
    /// <param name="projectId">The ID of the project this retro board belongs to.</param>
    /// <param name="name">The name of the retro board.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is null, empty, or whitespace.
    /// </exception>
    public RetroBoard(Guid projectId, string name)
    {
        ProjectId = projectId;
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
    }

    /// <summary>Gets the ID of the project this retro board belongs to.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>Gets the name of the retro board.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the navigation property to the owning project.</summary>
    public Project Project { get; private set; } = null!;

    /// <summary>Gets the read-only collection of columns in this retro board.</summary>
    public IReadOnlyCollection<Column> Columns => _columns.AsReadOnly();

    /// <summary>
    /// Adds a new column to this retro board, enforcing the unique-name invariant.
    /// </summary>
    /// <param name="name">The name of the column.</param>
    /// <returns>The created <see cref="Column"/> entity.</returns>
    /// <exception cref="Exceptions.InvariantViolationException">
    /// Thrown when a column with the same name already exists in this retro board.
    /// </exception>
    /// <remarks>
    /// DESIGN: This method is provided for completeness but is NOT used by the
    /// ColumnService in API 2. The service still uses a repository query for the
    /// uniqueness check because loading all columns to add one is wasteful without
    /// aggregate boundaries. API 3 makes this the primary creation path.
    /// </remarks>
    public Column AddColumn(string name)
    {
        if (_columns.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new Exceptions.InvariantViolationException(
                $"A column with name '{name}' already exists in this retro board.");

        var column = new Column(Id, name);
        _columns.Add(column);
        return column;
    }
}
