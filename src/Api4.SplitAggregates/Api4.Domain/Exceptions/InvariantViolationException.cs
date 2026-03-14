namespace Api4.Domain.Exceptions;

/// <summary>
/// Thrown when a domain invariant is violated (e.g., duplicate column name
/// within a retro board, duplicate vote, duplicate project member).
/// </summary>
/// <remarks>
/// DESIGN: In API 3, ALL invariant enforcement lived inside the aggregate
/// root — column name uniqueness, note text uniqueness, and vote uniqueness
/// were all checked by the RetroBoard aggregate root. In API 4, vote
/// uniqueness ("one vote per user per note") is no longer checked by the
/// aggregate root because Vote is its own aggregate. Instead, the VoteService
/// throws this exception based on an application-level check, and the DB
/// unique constraint acts as the ultimate safety net.
///
/// The global exception middleware maps this to HTTP 409 Conflict.
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
