namespace Api0b.WebApi.Exceptions;

/// <summary>
/// Thrown when a requested entity cannot be found in the database.
/// Mapped to HTTP 404 Not Found by the global exception handler middleware.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="NotFoundException"/>.
    /// </summary>
    /// <param name="entityName">The name of the entity type that was not found.</param>
    /// <param name="id">The ID that was searched for.</param>
    public NotFoundException(string entityName, Guid id)
        : base($"{entityName} with ID '{id}' was not found.")
    {
        EntityName = entityName;
        EntityId = id;
    }

    /// <summary>Gets the name of the entity type that was not found.</summary>
    public string EntityName { get; }

    /// <summary>Gets the ID that was searched for.</summary>
    public Guid EntityId { get; }
}

/// <summary>
/// Thrown when an operation would create a duplicate entity that violates
/// a uniqueness constraint.
/// Mapped to HTTP 409 Conflict by the global exception handler middleware.
/// </summary>
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

/// <summary>
/// Thrown when a business rule is violated (e.g., a user voting twice on the same note).
/// Mapped to HTTP 409 Conflict by the global exception handler middleware.
/// </summary>
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
