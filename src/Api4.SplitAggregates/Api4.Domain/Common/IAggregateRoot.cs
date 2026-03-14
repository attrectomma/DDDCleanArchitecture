namespace Api4.Domain.Common;

/// <summary>
/// Marker interface identifying an entity as an aggregate root.
/// Only aggregate roots may have repositories. Only aggregate roots
/// are loaded/saved as a whole.
/// </summary>
/// <remarks>
/// DESIGN: Same marker interface as API 3. In API 4, four entities carry this
/// marker: <c>User</c>, <c>Project</c>, <c>RetroBoard</c>, and <c>Vote</c>.
/// The key change is that <c>Vote</c> is now an aggregate root — in API 3
/// it was a child entity deep inside the RetroBoard aggregate. This means
/// Vote now has its own repository and its own concurrency token.
/// </remarks>
public interface IAggregateRoot
{
}
