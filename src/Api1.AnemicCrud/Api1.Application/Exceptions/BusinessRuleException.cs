namespace Api1.Application.Exceptions;

/// <summary>
/// Thrown when a business rule is violated (e.g., a user voting twice on the same note).
/// Mapped to HTTP 409 Conflict by the global exception handler middleware.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 this is a catch-all for domain rule violations that
/// don't fit into <see cref="DuplicateException"/> or <see cref="NotFoundException"/>.
/// In API 2+ some of these checks move into domain entity methods and throw
/// domain-specific exceptions instead.
/// </remarks>
public class BusinessRuleException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="BusinessRuleException"/>.
    /// </summary>
    /// <param name="message">A description of the business rule that was violated.</param>
    public BusinessRuleException(string message) : base(message)
    {
    }
}
