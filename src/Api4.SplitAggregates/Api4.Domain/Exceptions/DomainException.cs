namespace Api4.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-level errors. Thrown by aggregate roots
/// and entities when a domain operation fails (e.g., entity not found
/// within an aggregate's collection).
/// </summary>
/// <remarks>
/// DESIGN: Same as API 2/3. Domain exceptions signal that the domain itself
/// detected the error. In API 4, the RetroBoard aggregate root still throws
/// these for in-memory child look-ups (e.g., "column not found in this retro").
/// The new Vote aggregate root is simpler and does not throw domain exceptions
/// directly — cross-aggregate checks are handled by the VoteService.
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
