namespace Shared.Infrastructure.Messaging.Dapper.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Dapper.Inbox;

public static class DapperMessagingExtensions
{
    public static IServiceCollection AddDapperInbox<TFactory>(this IServiceCollection services)
        where TFactory : class, INpgsqlConnectionFactory
    {
        services.AddScoped<IInboxExecutor, DapperInboxExecutor<TFactory>>();
        return services;
    }
}
