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
          → TransactionCommandDispatcherDecorator  (wraps commands in an ambient TransactionScope)
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

The decorator is **module-agnostic** — it opens an ambient `System.Transactions.TransactionScope`
rather than a transaction on a specific `DbContext`. Whatever connection the handler opens
(EF `DbContext` or Dapper) enlists in the ambient transaction. A single command only ever
touches one module's database (cross-module writes are forbidden), so the scope never
promotes to a distributed transaction.

```csharp
// Shared.Infrastructure.Cqrs/Decorators/TransactionCommandDispatcherDecorator.cs
internal sealed class TransactionCommandDispatcherDecorator : ICommandDispatcher
{
    private readonly ICommandDispatcher _inner;

    public TransactionCommandDispatcherDecorator(ICommandDispatcher inner) => _inner = inner;

    public async Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand
    {
        using var scope = CreateScope();
        await _inner.SendAsync(command, ct);
        scope.Complete();
    }

    public async Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>
    {
        using var scope = CreateScope();
        var result = await _inner.SendAsync<TCommand, TResult>(command, ct);
        scope.Complete();
        return result;
    }

    private static TransactionScope CreateScope() => new(
        TransactionScopeOption.Required,
        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
        TransactionScopeAsyncFlowOption.Enabled);
}
```

> **Style-2 (Dapper) modules** build their `NpgsqlDataSource` with
> `ConnectionStringBuilder.Enlist = false` so their explicit `DapperUnitOfWork` transaction
> is not disturbed by the ambient scope.

---

## DI Registration

### Shared.Infrastructure.Cqrs extension

Two entry points, because the dispatcher chain is shared by every module:

- **`AddCqrsHandlers(params Assembly[])`** — each module calls this in its own
  infrastructure DI to scan its assemblies for handlers, validators and domain-event
  handlers.
- **`AddCqrsDispatchers()`** — the **host** calls this exactly once, after every module is
  registered, to build the `ICommandDispatcher` / `IQueryDispatcher` decorator chain.

```csharp
// Shared.Infrastructure.Cqrs/Extensions/CqrsExtensions.cs
public static class CqrsExtensions
{
    public static IServiceCollection AddCqrsDispatchers(this IServiceCollection services)
    {
        services.AddScoped<ICommandDispatcher>(sp =>
        {
            ICommandDispatcher d = new CommandDispatcher(sp);
            d = new TransactionCommandDispatcherDecorator(d);
            d = new ValidationCommandDispatcherDecorator(d, sp);
            d = new LoggingCommandDispatcherDecorator(
                d, sp.GetRequiredService<ILogger<LoggingCommandDispatcherDecorator>>());
            return d;
        });

        services.AddScoped<IQueryDispatcher>(sp =>
        {
            IQueryDispatcher d = new QueryDispatcher(sp);
            d = new LoggingQueryDispatcherDecorator(
                d, sp.GetRequiredService<ILogger<LoggingQueryDispatcherDecorator>>());
            return d;
        });

        return services;
    }

    public static IServiceCollection AddCqrsHandlers(
        this IServiceCollection services,
        params Assembly[] handlersAssemblies)
    {
        services.Scan(scan => scan
            .FromAssemblies(handlersAssemblies)
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(ICommandValidator<>)), publicOnly: false)
                .AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces().WithScopedLifetime());

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
    services.AddDbContext<BudgetPlanDbContext>((sp, opts) =>
        opts.UseNpgsql(configuration.GetConnectionString("BudgetPlan"))
            .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

    services.AddDomainEventDispatcher();      // idempotent — safe from every Style-1 module
    services.AddCqrsHandlers(typeof(CreateBudgetPlanCommandHandler).Assembly);
    services.AddOutbox<BudgetPlanDbContext>();

    services.AddScoped<IBudgetPlanRepository, BudgetPlanRepository>();

    // A second+ Style-1 module must NOT register the global IUnitOfWork (DietPlanner owns
    // that binding). Expose a module-scoped abstraction instead, e.g.:
    //   services.AddScoped<IBudgetPlanUnitOfWork>(sp => sp.GetRequiredService<BudgetPlanDbContext>());
    services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BudgetPlanDbContext>());

    return services;
}
```

```csharp
// HomeSystem.REST/Program.cs
builder.Services.AddBudgetPlanModule(builder.Configuration);
builder.Services.AddDietPlannerModule(builder.Configuration);
// ... every other module ...
builder.Services.AddCqrsDispatchers();   // once, after all modules
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
