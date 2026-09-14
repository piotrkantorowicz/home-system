namespace Shared.Abstractions.Messaging;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage(
    "Naming",
    "CA1711:Identifiers should not have incorrect suffix",
    Justification = "The 'EventHandler' suffix names the handler of a domain/integration event, not a System.EventHandler delegate; the name is a public contract used by every module.")]
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken ct = default);
}
