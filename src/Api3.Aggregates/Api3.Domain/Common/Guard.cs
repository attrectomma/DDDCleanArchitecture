namespace Api3.Domain.Common;

/// <summary>
/// Simple guard-clause utility used by domain entities to validate arguments.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 2. Guard clauses keep argument validation DRY across
/// entity constructors and methods. In API 3, guard clauses are used inside
/// aggregate root methods and child entity methods alike, ensuring that
/// every mutation within the aggregate boundary validates its input.
/// </remarks>
public static class Guard
{
    /// <summary>
    /// Ensures a string value is not null, empty, or whitespace.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="parameterName">The name of the parameter for the error message.</param>
    /// <returns>The validated string value.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is null, empty, or whitespace.
    /// </exception>
    public static string AgainstNullOrWhiteSpace(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} cannot be null or empty.", parameterName);

        return value;
    }
}
