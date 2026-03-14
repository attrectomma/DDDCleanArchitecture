namespace Api5.Domain.Exceptions;

/// <summary>
/// Thrown when a domain invariant is violated (e.g., duplicate column name
/// within a retro board, duplicate vote, duplicate project member).
/// </summary>
/// <remarks>
/// DESIGN: Same as API 3/4. In API 5, this exception is thrown both by
/// aggregate root methods (in-memory invariants like column name uniqueness)
/// and by command handlers (cross-aggregate checks like duplicate vote).
/// The global exception middleware maps this to HTTP 409 Conflict.
///
/// Compare with API 1 where all validation was in service methods and
/// API 2 where rich domain methods began enforcing invariants. API 5
/// preserves the same invariant enforcement from API 4 but the
/// orchestration moves from services to individual command handlers.
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
