namespace Api2.Domain.Exceptions;

/// <summary>
/// Thrown when a domain invariant is violated (e.g., duplicate vote,
/// duplicate project member, duplicate note text within a column).
/// </summary>
/// <remarks>
/// DESIGN: In API 1 these were Application-layer exceptions
/// (<c>DuplicateException</c> and <c>BusinessRuleException</c>).
/// Moving them to the Domain layer
/// signals that the domain itself is responsible for enforcing its rules.
/// The global exception middleware maps this to HTTP 409 Conflict.
///
/// Note that cross-entity invariants (e.g., column name uniqueness across
/// a retro board) still live in the service layer because the Column entity
/// doesn't know about its siblings. API 3 resolves this by making
/// RetroBoard an aggregate root that owns all columns.
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
