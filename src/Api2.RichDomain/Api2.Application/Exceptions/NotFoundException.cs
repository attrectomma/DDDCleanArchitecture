namespace Api2.Application.Exceptions;

/// <summary>
/// Thrown when a requested entity cannot be found in the database.
/// Mapped to HTTP 404 Not Found by the global exception handler middleware.
/// </summary>
/// <remarks>
/// DESIGN: In API 2 this exception remains in the Application layer because
/// "not found" is an application-level concern — the domain entities don't
/// do database lookups. Services throw this when a repository returns <c>null</c>.
/// Domain entities throw <see cref="Api2.Domain.Exceptions.DomainException"/>
/// for in-memory "not found" scenarios (e.g., vote not in collection).
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
