using Api5.Domain.VoteAggregate.Strategies;

namespace Api5.Application.Common.Options;

/// <summary>
/// Strongly-typed configuration options for voting behaviour.
/// Bound from the <c>Voting</c> section in <c>appsettings.json</c>.
/// </summary>
/// <remarks>
/// DESIGN: The Options pattern (<c>IOptions&lt;T&gt;</c>) provides a clean,
/// testable way to inject configuration into services and command handlers.
/// Combined with <see cref="Microsoft.Extensions.Options.IValidateOptions{TOptions}"/>,
/// it ensures misconfiguration is caught at application startup — not at runtime
/// when a user casts a vote.
///
/// This class centralises two values that were previously scattered:
/// <list type="bullet">
///   <item><see cref="DefaultVotingStrategy"/>: The fallback strategy when a
///   retro board is created without explicitly specifying one. Previously
///   hardcoded as <see cref="VotingStrategyType.Default"/> in the controller.</item>
///   <item><see cref="MaxVotesPerColumn"/>: The budget limit for the
///   <see cref="BudgetVotingStrategy"/>. Previously a <c>const</c> in
///   <c>BudgetVotingStrategy</c>; now externally configurable.</item>
/// </list>
///
/// Compare with API 1–4 where voting behaviour was entirely hardcoded.
/// API 5 demonstrates how the Options pattern makes behaviour configurable
/// without modifying business logic classes.
/// </remarks>
public class VotingOptions
{
    /// <summary>
    /// The configuration section name in <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "Voting";

    /// <summary>
    /// Gets or sets the default voting strategy applied to new retro boards
    /// when the caller does not specify one.
    /// </summary>
    /// <remarks>
    /// Supported values: <see cref="VotingStrategyType.Default"/> (one vote
    /// per user per note) and <see cref="VotingStrategyType.Budget"/> (dot
    /// voting with a per-column limit).
    /// </remarks>
    public VotingStrategyType DefaultVotingStrategy { get; set; } = VotingStrategyType.Default;

    /// <summary>
    /// Gets or sets the maximum number of votes a user may cast per column
    /// when using the <see cref="VotingStrategyType.Budget"/> strategy.
    /// </summary>
    /// <remarks>
    /// This value is ignored when the <see cref="VotingStrategyType.Default"/>
    /// strategy is in use. Must be greater than zero.
    /// </remarks>
    public int MaxVotesPerColumn { get; set; } = BudgetVotingStrategy.DefaultMaxVotesPerColumn;
}
