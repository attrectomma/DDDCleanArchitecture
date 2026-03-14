namespace Api2.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-level errors. Thrown by entities when a
/// domain operation fails (e.g., entity not found within a collection).
/// </summary>
/// <remarks>
/// DESIGN: In API 1, all exceptions lived in the Application layer
/// (<c>BusinessRuleException</c>, <c>DuplicateException</c>, etc.).
/// Moving exception types into the Domain layer signals that the domain
/// itself is responsible for enforcing its rules. The Application layer
/// catches these and translates them into appropriate HTTP responses
/// when needed. In API 3+ domain exceptions become even more important
/// as aggregate roots enforce complex cross-entity invariants.
/// </remarks>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="DomainException"/>.
    /// </summary>
    /// <param name="message">A description of the domain error.</param>
    public DomainException(string message) : base(message)
    {
    }
}
