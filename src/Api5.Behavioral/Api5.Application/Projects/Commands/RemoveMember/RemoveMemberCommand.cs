using Api5.Application.Common.Interfaces;
using MediatR;

namespace Api5.Application.Projects.Commands.RemoveMember;

/// <summary>
/// Command to remove a user from a project's membership.
/// </summary>
/// <param name="ProjectId">The ID of the project.</param>
/// <param name="UserId">The ID of the user to remove.</param>
public record RemoveMemberCommand(Guid ProjectId, Guid UserId) : ICommand<Unit>;
