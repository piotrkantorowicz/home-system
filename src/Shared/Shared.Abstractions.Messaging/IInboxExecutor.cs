namespace Shared.Abstractions.Messaging;

/// <summary>
/// Idempotent execution boundary for an integration-event handler in a consuming module.
/// Implementations open a transaction on the consumer's storage, check the inbox for the
/// given <paramref name="eventId"/>, invoke <paramref name="handlerInvocation"/> only on the
/// first delivery, persist the inbox marker, and commit. Implementations are owned by
/// concrete persistence packages (EF, Dapper) — never depend on a specific implementation
/// from outside those packages.
/// </summary>
public interface IInboxExecutor
{
    Task ExecuteAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handlerInvocation,
        CancellationToken ct = default);
}
