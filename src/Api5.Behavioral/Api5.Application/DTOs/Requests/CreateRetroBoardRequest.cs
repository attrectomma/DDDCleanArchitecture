using Api5.Domain.VoteAggregate.Strategies;

namespace Api5.Application.DTOs.Requests;

/// <summary>Request DTO for creating a new retro board within a project.</summary>
/// <param name="Name">The name of the retro board.</param>
/// <param name="VotingStrategy">
/// The voting strategy for the board. Defaults to <see cref="VotingStrategyType.Default"/>
/// when not specified. Supported values: "Default" (one vote per user per note)
/// and "Budget" (dot voting with a per-column vote limit).
/// </param>
public record CreateRetroBoardRequest(string Name, VotingStrategyType? VotingStrategy = null);
