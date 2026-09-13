namespace Shared.Infrastructure.Cqrs.Extensions;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Cqrs;
using Shared.Infrastructure.Cqrs.Decorators;

/// <summary>
/// DI registration for the custom CQRS stack: modules register their handlers with
/// <see cref="AddCqrsHandlers"/>, the host builds the shared dispatcher chain once with
/// <see cref="AddCqrsDispatchers"/>.
/// </summary>
public static class CqrsExtensions
{
    /// <summary>
    /// Registers the command/query dispatcher chain. Call once from the host, after every
    /// module has registered its handlers via <see cref="AddCqrsHandlers"/>. The chain is
    /// module-agnostic — the transaction decorator uses an ambient <c>TransactionScope</c>,
    /// not a specific <c>DbContext</c> — so any number of Style-1 and Style-2 modules share it.
    /// </summary>
    public static IServiceCollection AddCqrsDispatchers(this IServiceCollection services)
    {
        services.AddScoped<ICommandDispatcher>(sp =>
        {
            ICommandDispatcher dispatcher = new CommandDispatcher(sp);

            dispatcher = new TransactionCommandDispatcherDecorator(dispatcher);

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

    /// <summary>
    /// Scans a module's assemblies for command/query handlers, validators and domain-event
    /// handlers. Call once per module in its infrastructure DI. The dispatcher chain itself
    /// is registered once by the host via <see cref="AddCqrsDispatchers"/>.
    /// </summary>
    public static IServiceCollection AddCqrsHandlers(
        this IServiceCollection services,
        params Assembly[] handlersAssemblies)
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

        return services;
    }
}
