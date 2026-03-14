namespace Api5.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-level errors. Thrown by aggregate roots
/// and entities when a domain operation fails (e.g., entity not found
/// within an aggregate's collection).
/// </summary>
/// <remarks>
/// DESIGN: Same as API 2/3/4. Domain exceptions signal that the domain itself
/// detected the error. In API 5, command handlers catch or propagate these
/// exceptions — the MediatR pipeline does not add special handling for them.
/// The global exception middleware maps them to Problem Details responses.
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
