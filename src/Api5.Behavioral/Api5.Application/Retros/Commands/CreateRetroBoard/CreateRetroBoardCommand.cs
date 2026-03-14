using Api5.Application.DTOs.Responses;
using Api5.Domain.VoteAggregate.Strategies;
using MediatR;

namespace Api5.Application.Retros.Commands.CreateRetroBoard;

/// <summary>
/// Command to create a new retro board within a project.
/// </summary>
/// <remarks>
/// DESIGN: When <see cref="VotingStrategyType"/> is <c>null</c>, the handler
/// resolves the default from <c>VotingOptions.DefaultVotingStrategy</c>
/// (Options pattern). This keeps the controller thin — it passes the caller's
/// explicit choice or <c>null</c>, and the handler applies the configured default.
/// </remarks>
/// <param name="ProjectId">The ID of the project this retro belongs to.</param>
/// <param name="Name">The name of the retro board.</param>
/// <param name="VotingStrategyType">
/// The voting strategy for the board, or <c>null</c> to use the configured
/// default from <c>VotingOptions</c>.
/// </param>
public record CreateRetroBoardCommand(
    Guid ProjectId,
    string Name,
    VotingStrategyType? VotingStrategyType = null) : IRequest<RetroBoardResponse>;
