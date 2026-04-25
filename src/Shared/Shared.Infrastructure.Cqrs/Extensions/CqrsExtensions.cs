namespace Shared.Infrastructure.Cqrs.Extensions;

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Cqrs;
using Shared.Infrastructure.Cqrs.Decorators;

public static class CqrsExtensions
{
    public static IServiceCollection AddCqrs<TDbContext>(
        this IServiceCollection services,
        params Assembly[] handlersAssemblies)
        where TDbContext : DbContext
    {
        services.Scan(scan => scan
            .FromAssemblies(handlersAssemblies)
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(ICommandValidator<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        services.AddScoped<ICommandDispatcher>(sp =>
        {
            ICommandDispatcher dispatcher = new CommandDispatcher(sp);

            dispatcher = new TransactionCommandDispatcherDecorator<TDbContext>(
                dispatcher,
                sp.GetRequiredService<TDbContext>());

            dispatcher = new ValidationCommandDispatcherDecorator(dispatcher, sp);

            dispatcher = new LoggingCommandDispatcherDecorator(
                dispatcher,
                sp.GetRequiredService<ILogger<LoggingCommandDispatcherDecorator>>());

            return dispatcher;
        });

        services.AddScoped<IQueryDispatcher>(sp =>
        {
            IQueryDispatcher dispatcher = new QueryDispatcher(sp);

            dispatcher = new LoggingQueryDispatcherDecorator(
                dispatcher,
                sp.GetRequiredService<ILogger<LoggingQueryDispatcherDecorator>>());

            return dispatcher;
        });

        return services;
    }
}
