using Api5.Application.DTOs.Responses;
using MediatR;

namespace Api5.Application.Projects.Commands.AddMember;

/// <summary>
/// Command to add a user as a member to a project.
/// </summary>
/// <param name="ProjectId">The ID of the project to add the member to.</param>
/// <param name="UserId">The ID of the user to add.</param>
public record AddMemberCommand(Guid ProjectId, Guid UserId) : IRequest<ProjectMemberResponse>;
