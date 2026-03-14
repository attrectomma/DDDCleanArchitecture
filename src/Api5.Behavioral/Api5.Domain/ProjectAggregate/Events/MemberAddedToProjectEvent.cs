using Api5.Domain.Common;

namespace Api5.Domain.ProjectAggregate.Events;

/// <summary>
/// Raised when a user is added as a member to a project.
/// </summary>
/// <remarks>
/// DESIGN: This domain event enables decoupled reactions to membership changes.
/// For example, a handler could send a notification to the user, update a
/// search index, or log an audit trail — all without the Project aggregate
/// knowing about these side effects. In API 4, any such side effects would
/// have to be added inline to the ProjectService.AddMemberAsync method.
/// </remarks>
/// <param name="ProjectId">The ID of the project the member was added to.</param>
/// <param name="UserId">The ID of the user who was added.</param>
/// <param name="MembershipId">The ID of the created ProjectMember entity.</param>
public record MemberAddedToProjectEvent(Guid ProjectId, Guid UserId, Guid MembershipId) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
