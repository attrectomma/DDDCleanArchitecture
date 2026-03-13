namespace Api1.Application.Exceptions;

/// <summary>
/// Thrown when an operation would create a duplicate entity that violates
/// a uniqueness constraint (e.g., duplicate column name within a retro board).
/// Mapped to HTTP 409 Conflict by the global exception handler middleware.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 this exception is thrown by the service layer after
/// a check-then-act query detects a duplicate. This check is NOT atomic
/// and can be bypassed under concurrent access. API 3+ enforce uniqueness
/// within the aggregate boundary with proper concurrency control.
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
