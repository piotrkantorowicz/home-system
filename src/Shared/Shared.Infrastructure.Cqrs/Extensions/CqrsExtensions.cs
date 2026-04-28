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

    // Dapper-style modules don't have a DbContext for the transaction decorator —
    // their handlers manage commit lifecycle through DapperUnitOfWork directly.
    // The host registers the dispatcher chain via AddCqrs<TDbContext> for a Style-1
    // module; this overload only adds the additional handler/validator scan so
    // a Dapper module's handlers become resolvable from the same dispatcher.
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
