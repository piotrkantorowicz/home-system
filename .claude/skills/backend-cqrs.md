# Backend — CQRS Infrastructure (Full Source)

> Complete source for the custom CQRS dispatcher stack. Contracts live in
> `Shared.Abstractions.Cqrs`; implementations + decorators in `Shared.Infrastructure.Cqrs`.
> Do not reach for MediatR — this implementation is intentionally simple and explicit.

---

## Shared.Abstractions.Cqrs — Interfaces

### CQRS Marker Interfaces

```csharp
// Shared.Abstractions.Cqrs/ICommand.cs
public interface ICommand { }
public interface ICommand<out TResult> { }
```

```csharp
// Shared.Abstractions.Cqrs/IQuery.cs
public interface IQuery<out TResult> { }
```

```csharp
// Shared.Abstractions.Cqrs/ICommandHandler.cs
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}
```

```csharp
// Shared.Abstractions.Cqrs/IQueryHandler.cs
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}
```

### Dispatcher Interfaces

```csharp
// Shared.Abstractions.Cqrs/ICommandDispatcher.cs
public interface ICommandDispatcher
{
    Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand;

    Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>;
}
```

```csharp
// Shared.Abstractions.Cqrs/IQueryDispatcher.cs
public interface IQueryDispatcher
{
    Task<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResult>;
}
```

### Validation Interface

```csharp
// Shared.Abstractions.Cqrs/ICommandValidator.cs
public interface ICommandValidator<in TCommand>
{
    IEnumerable<ValidationError> Validate(TCommand command);
}

public sealed record ValidationError(string PropertyName, string ErrorMessage);
```

### Domain Event Handler Interface

```csharp
// Shared.Abstractions.Cqrs/IDomainEventHandler.cs
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken ct = default);
}
```

---

## Shared.Infrastructure.Cqrs — Implementations

### Command Dispatcher (base implementation)

```csharp
// Shared.Infrastructure.Cqrs/CommandDispatcher.cs
internal sealed class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
        return handler.HandleAsync(command, ct);
    }

    public Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        return handler.HandleAsync(command, ct);
    }
}
```

### Query Dispatcher (base implementation)

```csharp
// Shared.Infrastructure.Cqrs/QueryDispatcher.cs
internal sealed class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public Task<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResult>
    {
        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return handler.HandleAsync(query, ct);
    }
}
```

---

## Decorator Chain

Cross-cutting concerns wrap the base dispatchers using the **decorator pattern**.
Each decorator is registered in DI wrapping the previous one. Decorators are ordered:

```
Endpoint call
  → LoggingCommandDispatcherDecorator    (outermost — logs start + end + duration)
      → ValidationCommandDispatcherDecorator  (validates before passing through)
          → TransactionCommandDispatcherDecorator  (wraps commands in a DB transaction)
              → CommandDispatcher              (innermost — resolves + calls handler)
```

Queries only get the logging decorator — no transaction needed.

### Logging Decorator

```csharp
// Shared.Infrastructure.Cqrs/Decorators/LoggingCommandDispatcherDecorator.cs
internal sealed class LoggingCommandDispatcherDecorator : ICommandDispatcher
{
    private readonly ICommandDispatcher _inner;
    private readonly ILogger<LoggingCommandDispatcherDecorator> _logger;

    public LoggingCommandDispatcherDecorator(
        ICommandDispatcher inner,
        ILogger<LoggingCommandDispatcherDecorator> logger)
        => (_inner, _logger) = (inner, logger);

    public async Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand
    {
        var name = typeof(TCommand).Name;
        _logger.LogInformation("Executing command {CommandName}", name);
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.SendAsync(command, ct);
            _logger.LogInformation("Command {CommandName} executed in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command {CommandName} failed after {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>
    {
        var name = typeof(TCommand).Name;
        _logger.LogInformation("Executing command {CommandName}", name);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _inner.SendAsync<TCommand, TResult>(command, ct);
            _logger.LogInformation("Command {CommandName} executed in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command {CommandName} failed after {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
```

```csharp
// Shared.Infrastructure.Cqrs/Decorators/LoggingQueryDispatcherDecorator.cs
internal sealed class LoggingQueryDispatcherDecorator : IQueryDispatcher
{
    private readonly IQueryDispatcher _inner;
    private readonly ILogger<LoggingQueryDispatcherDecorator> _logger;

    public LoggingQueryDispatcherDecorator(
        IQueryDispatcher inner,
        ILogger<LoggingQueryDispatcherDecorator> logger)
        => (_inner, _logger) = (inner, logger);

    public async Task<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResult>
    {
        var name = typeof(TQuery).Name;
        _logger.LogInformation("Executing query {QueryName}", name);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _inner.SendAsync<TQuery, TResult>(query, ct);
            _logger.LogInformation("Query {QueryName} executed in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query {QueryName} failed after {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### Validation Decorator

```csharp
// Shared.Infrastructure.Cqrs/Decorators/ValidationCommandDispatcherDecorator.cs
internal sealed class ValidationCommandDispatcherDecorator : ICommandDispatcher
{
    private readonly ICommandDispatcher _inner;
    private readonly IServiceProvider _serviceProvider;

    public ValidationCommandDispatcherDecorator(
        ICommandDispatcher inner,
        IServiceProvider serviceProvider)
        => (_inner, _serviceProvider) = (inner, serviceProvider);

    public Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand
    {
        Validate(command);
        return _inner.SendAsync(command, ct);
    }

    public Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>
    {
        Validate(command);
        return _inner.SendAsync<TCommand, TResult>(command, ct);
    }

    private void Validate<TCommand>(TCommand command)
    {
        var validator = _serviceProvider.GetService<ICommandValidator<TCommand>>();
        if (validator is null)
            return;

        var errors = validator.Validate(command).ToList();
        if (errors.Count > 0)
            throw new CommandValidationException(typeof(TCommand).Name, errors);
    }
}
```

```csharp
// Shared.Abstractions.Cqrs/CommandValidationException.cs
public sealed class CommandValidationException : Exception
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public CommandValidationException(string commandName, IReadOnlyList<ValidationError> errors)
        : base($"Validation failed for command '{commandName}'.")
        => Errors = errors;
}
```

### Transaction Decorator

```csharp
// Shared.Infrastructure.Cqrs/Decorators/TransactionCommandDispatcherDecorator.cs
// Wraps each command in a DB transaction using the module's DbContext.
// Register per-module with the correct TDbContext type parameter.
internal sealed class TransactionCommandDispatcherDecorator<TDbContext> : ICommandDispatcher
    where TDbContext : DbContext
{
    private readonly ICommandDispatcher _inner;
    private readonly TDbContext _dbContext;

    public TransactionCommandDispatcherDecorator(
        ICommandDispatcher inner,
        TDbContext dbContext)
        => (_inner, _dbContext) = (inner, dbContext);

    public async Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
        await _inner.SendAsync(command, ct);
        await tx.CommitAsync(ct);
    }

    public async Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(ct);
        var result = await _inner.SendAsync<TCommand, TResult>(command, ct);
        await tx.CommitAsync(ct);
        return result;
    }
}
```

---

## DI Registration

### Shared.Infrastructure.Cqrs extension

```csharp
// Shared.Infrastructure.Cqrs/Extensions/CqrsExtensions.cs
public static class CqrsExtensions
{
    /// <summary>
    /// Registers the CQRS dispatcher stack for a given module assembly.
    /// Call once per module in the module's DependencyInjection.cs.
    /// </summary>
    public static IServiceCollection AddCqrs<TDbContext>(
        this IServiceCollection services,
        Assembly handlersAssembly)
        where TDbContext : DbContext
    {
        // Register all handlers from the module's Application assembly
        services.Scan(scan => scan
            .FromAssemblies(handlersAssembly)
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

        // Command dispatcher — innermost first, outermost registered last (wraps previous)
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

        // Query dispatcher — logging only
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
```

> **Note:** This uses [Scrutor](https://github.com/khellang/Scrutor) for assembly scanning
> (`services.Scan`). Already referenced from `Shared.Infrastructure.Cqrs`.

### Module usage

```csharp
// BudgetPlan.Infrastructure/DependencyInjection.cs
internal static IServiceCollection AddBudgetPlanInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddDbContext<BudgetPlanDbContext>(opts =>
        opts.UseNpgsql(configuration.GetConnectionString("BudgetPlan")));

    // Registers all handlers + dispatchers for this module
    services.AddCqrs<BudgetPlanDbContext>(
        typeof(CreateBudgetPlanCommandHandler).Assembly);

    services.AddScoped<IBudgetPlanRepository, BudgetPlanRepository>();
    services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BudgetPlanDbContext>());

    return services;
}
```

---

## EF Core Domain Event Dispatcher (SaveChanges Interceptor)

Domain events are dispatched within the same transaction as the `SaveChanges` call.

```csharp
// Shared.Infrastructure.Persistence/DomainEventDispatcherInterceptor.cs
internal sealed class DomainEventDispatcherInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcherInterceptor(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken ct = default)
    {
        await DispatchDomainEventsAsync(eventData.Context!, ct);
        return await base.SavedChangesAsync(eventData, result, ct);
    }

    private async Task DispatchDomainEventsAsync(DbContext dbContext, CancellationToken ct)
    {
        var aggregates = dbContext.ChangeTracker
            .Entries<AggregateRoot<object>>()   // or use a marker interface IAggregateRoot
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                await (Task)handlerType
                    .GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!
                    .Invoke(handler, [domainEvent, ct])!;
            }
        }
    }
}
```

Register the interceptor in each module's DbContext:

```csharp
// BudgetPlan.Infrastructure/DependencyInjection.cs
services.AddScoped<DomainEventDispatcherInterceptor>();

services.AddDbContext<BudgetPlanDbContext>((sp, opts) =>
    opts.UseNpgsql(configuration.GetConnectionString("BudgetPlan"))
        .AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>()));
```

---

## Error Middleware — Handling Validation Exceptions

```csharp
// Shared.Infrastructure.Web/ExceptionHandlingMiddleware.cs (relevant excerpt)
catch (CommandValidationException ex)
{
    context.Response.StatusCode = StatusCodes.Status400BadRequest;
    await context.Response.WriteAsJsonAsync(new ValidationProblemDetails
    {
        Title = "Validation failed",
        Errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray())
    });
}
```
