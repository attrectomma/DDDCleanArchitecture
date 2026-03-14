namespace Api5.Domain.VoteAggregate.Strategies;

/// <summary>
/// Defines the available voting strategies that a <see cref="RetroAggregate.RetroBoard"/>
/// can use to control how votes are cast.
/// </summary>
/// <remarks>
/// DESIGN: The Strategy pattern replaces conditional logic with polymorphism.
/// Instead of checking <c>if (strategyType == ...)</c> inside the command handler,
/// each strategy type maps to a concrete <see cref="IVotingStrategy"/> implementation
/// via <see cref="VotingStrategyFactory"/>. Adding a new strategy means adding a new
/// enum value and a new class — no existing code is modified (Open/Closed Principle).
///
/// This enum is stored on the RetroBoard aggregate so each board can independently
/// choose its voting behaviour. The value is persisted as a string in the database
/// for readability.
/// </remarks>
public enum VotingStrategyType
{
    /// <summary>
    /// Default voting strategy: one vote per user per note.
    /// This is the same behaviour as API 1–4.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Budget voting strategy: each user gets a fixed number of votes per column
    /// (default: 3) and may place multiple votes on the same note. This is commonly
    /// known as "dot voting" in agile retrospectives.
    /// </summary>
    Budget = 1
}
