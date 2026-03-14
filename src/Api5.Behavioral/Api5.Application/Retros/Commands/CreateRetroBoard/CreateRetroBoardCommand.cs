using Api5.Application.DTOs.Responses;
using MediatR;

namespace Api5.Application.Retros.Commands.CreateRetroBoard;

/// <summary>
/// Command to create a new retro board within a project.
/// </summary>
/// <param name="ProjectId">The ID of the project this retro belongs to.</param>
/// <param name="Name">The name of the retro board.</param>
public record CreateRetroBoardCommand(Guid ProjectId, string Name) : IRequest<RetroBoardResponse>;
