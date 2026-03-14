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
/// </remarks>
public static class VotingStrategyFactory
{
    /// <summary>
    /// Creates an <see cref="IVotingStrategy"/> for the specified strategy type.
    /// </summary>
    /// <param name="strategyType">The voting strategy type configured on the retro board.</param>
    /// <returns>The corresponding voting strategy implementation.</returns>
    /// <exception cref="DomainException">
    /// Thrown when the strategy type is not recognised.
    /// </exception>
    public static IVotingStrategy Create(VotingStrategyType strategyType) => strategyType switch
    {
        VotingStrategyType.Default => new DefaultVotingStrategy(),
        VotingStrategyType.Budget => new BudgetVotingStrategy(),
        _ => throw new DomainException($"Unknown voting strategy type: {strategyType}.")
    };
}
