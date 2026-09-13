namespace Shared.Infrastructure.Messaging.Extensions;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Fluent handle returned by <see cref="MessagingExtensions.AddIntegrationEventBus"/> so the host can
/// pick a transport (<see cref="MessagingExtensions.UseInProcessTransport"/> today, RabbitMQ later)
/// without the choice leaking into module code.
/// </summary>
public sealed class MessagingBuilder
{
    /// <summary>The service collection the messaging stack is being registered into.</summary>
    public IServiceCollection Services { get; }

    /// <summary>Wraps the host's service collection.</summary>
    /// <param name="services">The collection <c>AddIntegrationEventBus</c> was called on.</param>
    public MessagingBuilder(IServiceCollection services) => Services = services;
}
