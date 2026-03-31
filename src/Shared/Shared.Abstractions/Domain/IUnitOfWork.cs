namespace Shared.Abstractions.Domain;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct = default);
}
