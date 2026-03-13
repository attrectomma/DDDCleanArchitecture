namespace Api1.Application.Exceptions;

/// <summary>
/// Thrown when a requested entity cannot be found in the database.
/// Mapped to HTTP 404 Not Found by the global exception handler middleware.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 this exception is thrown by service methods when
/// a repository lookup returns <c>null</c>. The controller does not
/// handle 404 logic — it delegates entirely to the service.
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
