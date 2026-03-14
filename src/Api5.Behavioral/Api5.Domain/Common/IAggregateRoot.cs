namespace Api5.Domain.Common;

/// <summary>
/// Marker interface identifying an entity as an aggregate root.
/// Only aggregate roots may have repositories. Only aggregate roots
/// are loaded/saved as a whole.
/// </summary>
/// <remarks>
/// DESIGN: Same marker as API 3/4. In API 5, four entities carry this
/// marker: <c>User</c>, <c>Project</c>, <c>RetroBoard</c>, and <c>Vote</c>.
/// The aggregate structure is identical to API 4 — the change in API 5 is
/// in how we interact with aggregates (commands/queries via MediatR) and
/// how we handle side effects (domain events), not in aggregate boundaries.
/// </remarks>
public interface IAggregateRoot
{
}
