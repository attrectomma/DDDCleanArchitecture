using Api5.Domain.Common;

namespace Api5.Domain.ProjectAggregate.Events;

/// <summary>
/// Raised when a user is removed from a project's membership.
/// </summary>
/// <remarks>
/// DESIGN: Handlers could revoke the user's access to the project's retro
/// boards, clean up related data, or notify the team. The Project aggregate
/// only declares that removal happened — it does not orchestrate the reaction.
/// </remarks>
/// <param name="ProjectId">The ID of the project the member was removed from.</param>
/// <param name="UserId">The ID of the user who was removed.</param>
public record MemberRemovedFromProjectEvent(Guid ProjectId, Guid UserId) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
