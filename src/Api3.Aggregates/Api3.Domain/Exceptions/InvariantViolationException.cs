namespace Api3.Domain.Exceptions;

/// <summary>
/// Thrown when a domain invariant is violated (e.g., duplicate column name
/// within a retro board, duplicate vote, duplicate project member).
/// </summary>
/// <remarks>
/// DESIGN: In API 1 these were Application-layer exceptions. In API 2, they
/// moved to Domain but some cross-entity checks still lived in the service
/// layer. In API 3, ALL invariant enforcement lives inside the aggregate
/// root — column name uniqueness, note text uniqueness, and vote uniqueness
/// are all checked by the RetroBoard aggregate root (or its child entities
/// reachable through the root). The global exception middleware maps this
/// to HTTP 409 Conflict.
/// </remarks>
public class InvariantViolationException : DomainException
{
    /// <summary>
    /// Initializes a new instance of <see cref="InvariantViolationException"/>.
    /// </summary>
    /// <param name="message">A description of the invariant that was violated.</param>
    public InvariantViolationException(string message) : base(message)
    {
    }
}
