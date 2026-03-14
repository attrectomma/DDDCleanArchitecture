namespace Api3.Domain.Common;

/// <summary>
/// Marker interface identifying an entity as an aggregate root.
/// Only aggregate roots may have repositories. Only aggregate roots
/// are loaded/saved as a whole.
/// </summary>
/// <remarks>
/// DESIGN: This marker interface is new in API 3. In API 1/2, any entity
/// could have its own repository. With aggregate design, only classes
/// marked as <see cref="IAggregateRoot"/> get a repository. Child entities
/// are always accessed through the root. This enforces the DDD rule that
/// the aggregate root is the consistency boundary.
/// </remarks>
public interface IAggregateRoot
{
}
