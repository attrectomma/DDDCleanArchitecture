using Api5.Domain.VoteAggregate.Strategies;

namespace Api5.Application.DTOs.Requests;

/// <summary>Request DTO for changing the voting strategy of a retro board.</summary>
/// <param name="VotingStrategy">
/// The new voting strategy. Supported values:
/// <see cref="VotingStrategyType.Default"/> (one vote per user per note) and
/// <see cref="VotingStrategyType.Budget"/> (dot voting with per-column limit).
/// </param>
public record ChangeVotingStrategyRequest(VotingStrategyType VotingStrategy);
