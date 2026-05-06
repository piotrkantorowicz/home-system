namespace Notifications.Application.Channels;

public interface INotificationConnectionRegistry
{
    void Track(string userId);
    void Untrack(string userId);
    bool IsOnline(string userId);
}
