namespace Api5.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested entity cannot be found in the database.
/// Mapped to HTTP 404 Not Found by the global exception handler middleware.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 3/4. "Not found" remains an application-level concern.
/// In API 5, command handlers throw this when an aggregate or cross-aggregate
/// entity is not found. Query handlers also throw this when a projected
/// entity is not found.
/// </remarks>
public class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="NotFoundException"/>.
    /// </summary>
    /// <param name="entityName">The name of the entity type that was not found.</param>
    /// <param name="id">The ID that was searched for.</param>
    public NotFoundException(string entityName, Guid id)
        : base($"{entityName} with ID '{id}' was not found.")
    {
        EntityName = entityName;
        EntityId = id;
    }

    /// <summary>Gets the name of the entity type that was not found.</summary>
    public string EntityName { get; }

    /// <summary>Gets the ID that was searched for.</summary>
    public Guid EntityId { get; }
}
