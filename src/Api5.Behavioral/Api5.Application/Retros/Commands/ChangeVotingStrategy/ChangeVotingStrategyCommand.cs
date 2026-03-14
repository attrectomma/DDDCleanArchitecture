using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using Api5.Domain.VoteAggregate.Strategies;

namespace Api5.Application.Retros.Commands.ChangeVotingStrategy;

/// <summary>
/// Command to change the voting strategy of a retro board.
/// </summary>
/// <remarks>
/// DESIGN: This command modifies the RetroBoard aggregate's configuration.
/// Existing votes are not re-validated — only future vote attempts use
/// the new strategy. See <see cref="ChangeVotingStrategyCommandHandler"/>
/// for the rationale.
/// </remarks>
/// <param name="RetroBoardId">The ID of the retro board to update.</param>
/// <param name="VotingStrategyType">The new voting strategy type.</param>
public record ChangeVotingStrategyCommand(
    Guid RetroBoardId,
    VotingStrategyType VotingStrategyType) : ICommand<RetroBoardResponse>;
