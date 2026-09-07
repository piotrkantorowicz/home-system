namespace Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// Ambient marker identifying which module's outbox store the integration-event bus should
/// write to for the current logical operation. Set by the domain-event dispatch boundary
/// (the EF <c>SaveChanges</c> interceptor) to the publishing module's <c>DbContext</c> type,
/// so <c>OutboxIntegrationEventBus</c> can resolve the keyed <c>IOutboxStore</c> that shares
/// that module's transaction. Flows across async calls via <see cref="AsyncLocal{T}"/>.
/// </summary>
public static class OutboxScope
{
    private static readonly AsyncLocal<object?> Key = new();

    /// <summary>
    /// The key (a module's <c>DbContext</c> <see cref="System.Type"/>) of the outbox store to
    /// use, or <c>null</c> when not inside a domain-event dispatch.
    /// </summary>
    public static object? CurrentKey
    {
        get => Key.Value;
        set => Key.Value = value;
    }
}
