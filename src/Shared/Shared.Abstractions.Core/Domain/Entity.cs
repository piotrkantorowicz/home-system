namespace Shared.Abstractions.Core.Domain;

public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;
}
