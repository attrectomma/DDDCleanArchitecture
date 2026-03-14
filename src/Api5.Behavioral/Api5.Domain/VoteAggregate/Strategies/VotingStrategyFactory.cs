using Api5.Domain.Exceptions;

namespace Api5.Domain.VoteAggregate.Strategies;

/// <summary>
/// Factory that creates the appropriate <see cref="IVotingStrategy"/>
/// implementation for a given <see cref="VotingStrategyType"/>.
/// </summary>
/// <remarks>
/// DESIGN: The factory pattern bridges the gap between the enum stored on
/// the RetroBoard aggregate and the polymorphic strategy object. The command
/// handler calls <see cref="Create"/> with the board's strategy type and
/// receives a ready-to-use strategy instance.
///
/// This factory is intentionally a static method rather than a DI-registered
/// service because the strategies are pure domain objects with no dependencies.
/// If a future strategy needs infrastructure dependencies (e.g., external
/// configuration), the factory could be promoted to an injected service.
///
/// DESIGN: The <c>maxVotesPerColumn</c> parameter is sourced from
/// <c>VotingOptions</c> via the Options pattern. This allows the budget limit
/// to be configured externally (e.g., in <c>appsettings.json</c>) instead of
/// being hardcoded in the strategy class. The factory passes it through to
/// <see cref="BudgetVotingStrategy"/> — for <see cref="DefaultVotingStrategy"/>
/// the parameter is ignored.
/// </remarks>
public static class VotingStrategyFactory
{
    /// <summary>
    /// Creates an <see cref="IVotingStrategy"/> for the specified strategy type.
    /// </summary>
    /// <param name="strategyType">The voting strategy type configured on the retro board.</param>
    /// <param name="maxVotesPerColumn">
    /// The maximum votes per user per column for the <see cref="BudgetVotingStrategy"/>.
    /// Defaults to <see cref="BudgetVotingStrategy.DefaultMaxVotesPerColumn"/> when not specified.
    /// Ignored for the <see cref="DefaultVotingStrategy"/>.
    /// </param>
    /// <returns>The corresponding voting strategy implementation.</returns>
    /// <exception cref="DomainException">
    /// Thrown when the strategy type is not recognised.
    /// </exception>
    public static IVotingStrategy Create(
        VotingStrategyType strategyType,
        int maxVotesPerColumn = BudgetVotingStrategy.DefaultMaxVotesPerColumn) => strategyType switch
    {
        VotingStrategyType.Default => new DefaultVotingStrategy(),
        VotingStrategyType.Budget => new BudgetVotingStrategy(maxVotesPerColumn),
        _ => throw new DomainException($"Unknown voting strategy type: {strategyType}.")
    };
}
