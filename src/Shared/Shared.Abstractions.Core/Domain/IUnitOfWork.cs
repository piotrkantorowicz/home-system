namespace Shared.Abstractions.Core.Domain;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct = default);
}
