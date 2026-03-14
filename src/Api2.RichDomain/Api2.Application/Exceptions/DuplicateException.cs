namespace Api2.Application.Exceptions;

/// <summary>
/// Thrown when an operation would create a duplicate entity that violates
/// a uniqueness constraint (e.g., duplicate column name within a retro board).
/// Mapped to HTTP 409 Conflict by the global exception handler middleware.
/// </summary>
/// <remarks>
/// DESIGN: In API 2 this exception is still used for cross-entity uniqueness
/// checks that remain in the service layer (e.g., column name uniqueness
/// across a retro board, note text uniqueness for updates). Within-entity
/// uniqueness checks now throw <see cref="Api2.Domain.Exceptions.InvariantViolationException"/>
/// from the domain layer instead.
/// </remarks>
public class DuplicateException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="DuplicateException"/>.
    /// </summary>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="propertyName">The property that has a duplicate value.</param>
    /// <param name="value">The duplicate value.</param>
    public DuplicateException(string entityName, string propertyName, string value)
        : base($"A {entityName} with {propertyName} '{value}' already exists.")
    {
        EntityName = entityName;
        PropertyName = propertyName;
        DuplicateValue = value;
    }

    /// <summary>Gets the name of the entity type.</summary>
    public string EntityName { get; }

    /// <summary>Gets the property name that has a duplicate value.</summary>
    public string PropertyName { get; }

    /// <summary>Gets the duplicate value.</summary>
    public string DuplicateValue { get; }
}
