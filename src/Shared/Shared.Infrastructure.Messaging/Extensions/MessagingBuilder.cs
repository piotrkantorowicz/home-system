namespace Shared.Infrastructure.Messaging.Extensions;

using Microsoft.Extensions.DependencyInjection;

public sealed class MessagingBuilder
{
    public IServiceCollection Services { get; }

    public MessagingBuilder(IServiceCollection services) => Services = services;
}
