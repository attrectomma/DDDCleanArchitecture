using Api5.Domain.VoteAggregate.Strategies;
using Microsoft.Extensions.Options;

namespace Api5.Application.Common.Options;

/// <summary>
/// Validates <see cref="VotingOptions"/> at application startup using the
/// <see cref="IValidateOptions{TOptions}"/> pattern.
/// </summary>
/// <remarks>
/// DESIGN: Options validation is a critical best practice that this
/// repository demonstrates. Without validation, a typo in
/// <c>appsettings.json</c> (e.g., <c>"DefaultVotingStrategy": "Bugdet"</c>)
/// would silently deserialise as the default enum value (0 = Default),
/// or worse, cause a runtime exception when the first vote is cast.
///
/// By implementing <see cref="IValidateOptions{TOptions}"/> and registering
/// it with <c>ValidateOnStart()</c>, the application fails fast during
/// startup if the configuration is invalid. This is far better than
/// discovering the problem at runtime.
///
/// Compare with <c>DataAnnotations</c>-based validation
/// (<c>ValidateDataAnnotations()</c>): <c>IValidateOptions&lt;T&gt;</c>
/// gives full control over validation logic and error messages, supports
/// cross-property validation, and keeps validation logic testable.
/// </remarks>
public class VotingOptionsValidator : IValidateOptions<VotingOptions>
{
    /// <summary>
    /// Validates the <see cref="VotingOptions"/> configuration.
    /// </summary>
    /// <param name="name">The options instance name (usually <c>Options.DefaultName</c>).</param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>
    /// <see cref="ValidateOptionsResult.Success"/> if valid; otherwise a failure result
    /// with a descriptive error message.
    /// </returns>
    public ValidateOptionsResult Validate(string? name, VotingOptions options)
    {
        if (!Enum.IsDefined(typeof(VotingStrategyType), options.DefaultVotingStrategy))
        {
            return ValidateOptionsResult.Fail(
                $"Voting:DefaultVotingStrategy value '{options.DefaultVotingStrategy}' is not a valid " +
                $"VotingStrategyType. Supported values: {string.Join(", ", Enum.GetNames<VotingStrategyType>())}.");
        }

        if (options.MaxVotesPerColumn <= 0)
        {
            return ValidateOptionsResult.Fail(
                $"Voting:MaxVotesPerColumn must be greater than 0, but was {options.MaxVotesPerColumn}.");
        }

        return ValidateOptionsResult.Success;
    }
}
