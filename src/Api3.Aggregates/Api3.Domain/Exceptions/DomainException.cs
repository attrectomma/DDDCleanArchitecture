namespace Api3.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-level errors. Thrown by aggregate roots
/// and entities when a domain operation fails (e.g., entity not found
/// within an aggregate's collection).
/// </summary>
/// <remarks>
/// DESIGN: Same as API 2. Domain exceptions signal that the domain itself
/// detected the error. In API 3, these are thrown more frequently because
/// the aggregate root owns all child look-ups. For example, "column not
/// found in this retro board" is a domain error thrown by the aggregate
/// root, not by a service doing a repository query.
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
