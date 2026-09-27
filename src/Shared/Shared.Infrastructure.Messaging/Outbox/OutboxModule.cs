namespace Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// Registered once per <c>AddOutbox&lt;TDbContext&gt;()</c> so admin tooling can enumerate the
/// publishing modules and resolve their keyed <see cref="IOutboxDeadLetterStore"/>.
/// </summary>
/// <param name="Name">Module name: the <c>DbContext</c> type name without the <c>DbContext</c> suffix (e.g. <c>Household</c>).</param>
/// <param name="Key">The <c>DbContext</c> type the module's stores are keyed by.</param>
public sealed record OutboxModule(string Name, Type Key);
