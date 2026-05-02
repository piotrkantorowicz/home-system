namespace Notifications.Domain.Abstractions;

public interface INotificationsUnitOfWork
{
    Task CommitAsync(CancellationToken ct = default);
}
