namespace Shared.Infrastructure.Messaging.Ef.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Ef.Inbox;
using Shared.Infrastructure.Messaging.Ef.Outbox;
using Shared.Infrastructure.Messaging.Outbox;

public static class EfMessagingExtensions
{
    public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IOutboxStore, EfOutboxStore<TDbContext>>();
        return services;
    }

    public static IServiceCollection AddInbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IInboxExecutor, EfInboxExecutor<TDbContext>>();
        return services;
    }
}
