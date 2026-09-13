namespace Shared.Abstractions.Core.Domain;

/// <summary>
/// Base class for an entity that lives inside an aggregate: it has identity but no lifecycle of its
/// own and is only ever created or changed through its owning <see cref="AggregateRoot{TId}"/>.
/// </summary>
/// <typeparam name="TId">The typed identifier record of the entity.</typeparam>
public abstract class Entity<TId>
{
    /// <summary>
    /// The entity's identity. Assigned once by the owning aggregate's factory; the setter is
    /// protected so only the entity and EF Core can populate it.
    /// </summary>
    public TId Id { get; protected set; } = default!;
}
