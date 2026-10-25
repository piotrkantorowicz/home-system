# Backend — Module Structure (Style 1: DDD + EF Core)

> Companion: `backend-dapper-module-structure.md` for Style-2 (Lightweight + Dapper) modules.
> See `backend-persistence-styles.md` for the decision criteria.

## Solution Layout

```
src/
  Apis/
    HomeSystem.REST/                       # Host project — wires modules, no business logic
  Modules/
    {ModuleName}/
      {ModuleName}.Api/
      {ModuleName}.Mcp/                    # Optional — only when the module exposes MCP tools
      {ModuleName}.Application/
      {ModuleName}.Contracts/
      {ModuleName}.Domain/
      {ModuleName}.Infrastructure/
      {ModuleName}.UnitTests/              # Co-located with the module
      {ModuleName}.IntegrationTests/
  Shared/
    Shared.Abstractions.Core/              # IDomainEvent, AggregateRoot, Entity, IUnitOfWork, exceptions, PagedList
    Shared.Abstractions.Cqrs/              # ICommand, IQuery, dispatchers, handlers, validators
    Shared.Abstractions.Messaging/         # IIntegrationEvent, IIntegrationEventBus, IIntegrationEventHandler, IInboxExecutor
    Shared.Infrastructure.Cqrs/            # CQRS dispatcher implementation + decorators
    Shared.Infrastructure.Messaging/       # Outbox bus, worker, in-process transport, JSON serializer
    Shared.Infrastructure.Messaging.Ef/    # EF outbox + inbox executor (parameterised on TDbContext)
    Shared.Infrastructure.Messaging.Dapper/# Dapper inbox executor (parameterised on INpgsqlConnectionFactory)
    Shared.Infrastructure.Persistence/     # EF Core interceptors (DomainEventDispatcherInterceptor)
    Shared.Infrastructure.Web/             # Cross-cutting web (ApplicationExceptionHandler, SecurityHeadersMiddleware)
```

## Full Module Anatomy (DDD Module)

```
Modules/BudgetPlan/
  BudgetPlan.Domain/
    Aggregates/
      BudgetPlan.cs
      BudgetPlanExceptions.cs       # Domain exceptions for this aggregate
    Entities/
      BudgetEntry.cs
    ValueObjects/
      BudgetPlanId.cs
      BudgetEntryId.cs
      Money.cs
      DateRange.cs
    Events/
      BudgetPlanCreatedDomainEvent.cs
      BudgetEntryAddedDomainEvent.cs
    Repositories/
      IBudgetPlanRepository.cs
    Services/                       # Domain services — only when logic doesn't fit aggregate

  BudgetPlan.Application/
    Commands/
      CreateBudgetPlan/
        CreateBudgetPlanCommand.cs
        CreateBudgetPlanCommandHandler.cs
        CreateBudgetPlanCommandValidator.cs
      AddBudgetEntry/
        AddBudgetEntryCommand.cs
        AddBudgetEntryCommandHandler.cs
        AddBudgetEntryCommandValidator.cs
    Queries/
      GetBudgetPlan/
        GetBudgetPlanQuery.cs
        GetBudgetPlanQueryHandler.cs
        BudgetPlanDto.cs
      ListBudgetPlans/
        ListBudgetPlansQuery.cs
        ListBudgetPlansQueryHandler.cs
        BudgetPlanSummaryDto.cs
    EventHandlers/
      BudgetPlanCreatedDomainEventHandler.cs  # maps domain event → integration event

  BudgetPlan.Contracts/
    Events/
      BudgetPlanCreatedIntegrationEvent.cs
    Interfaces/
      IBudgetPlanQueryService.cs    # public query surface for other modules (if needed)

  BudgetPlan.Infrastructure/
    Persistence/
      BudgetPlanDbContext.cs
      Configurations/
        BudgetPlanConfiguration.cs
        BudgetEntryConfiguration.cs
      Repositories/
        BudgetPlanRepository.cs
      Migrations/
    Messaging/
      Consumers/
        UserDeletedIntegrationEventHandler.cs   # IIntegrationEventHandler<T>
    DependencyInjection.cs          # internal wiring

  BudgetPlan.Api/
    BudgetPlanEndpoints.cs
    DependencyInjection.cs          # AddBudgetPlanModule() public extension method

  BudgetPlan.Mcp/                   # Optional: second transport for agents (see backend-api-patterns.md)
    BudgetPlanMcp.cs                # scopes + AddBudgetPlanMcpTools(this IMcpServerBuilder)
    McpCaller.cs                    # scoped caller from McpToolUser
    EnvelopeTools.cs                # [McpServerToolType] classes, one per area
    EnvelopeSummaryDto.cs           # only where the agent-facing shape diverges
```

### The `.Mcp` project

MCP tools live in `{Module}.Mcp`, never in `{Module}.Api`: the REST project carries no MCP
SDK and no scope policies. The host composes it (`AddMcpServer()…Add{Module}McpTools()`).

- **References:** `{Module}.Application` and `Shared.Infrastructure.Web` (`McpToolUser`,
  `McpToolInvoker`). Never `{Module}.Api`, `Domain` or `Infrastructure`.
- **DTOs:** a transport gets its own DTO only when its shape diverges from the Application
  DTO (flattened, renamed for the agent, fields dropped); otherwise return the Application DTO.
- `InternalsVisibleTo` the module's integration-test project, like `.Api`.

## Simple CRUD Module Anatomy

```
Modules/UserPreferences/
  UserPreferences.Application/
    Commands/
      UpsertPreference/
        UpsertPreferenceCommand.cs
        UpsertPreferenceCommandHandler.cs
    Queries/
      GetPreferences/
        GetPreferencesQuery.cs
        GetPreferencesQueryHandler.cs
        PreferencesDto.cs
  UserPreferences.Infrastructure/
    Persistence/
      UserPreferencesDbContext.cs
      Configurations/
        UserPreferenceConfiguration.cs
      Repositories/
        UserPreferenceRepository.cs
      Migrations/
  UserPreferences.Api/
    UserPreferencesEndpoints.cs
    DependencyInjection.cs
```

No Domain project, no Contracts project unless other modules need to consume events from it.

## Layer Dependency Rules

```
Domain              ← depends on Shared.Abstractions.Core only
Application         ← depends on Domain, Shared.Abstractions.{Core,Cqrs,Messaging}
Infrastructure      ← depends on Domain, Application, Shared.Infrastructure.{Cqrs,Messaging,Messaging.Ef,Persistence}, EF Core
Api                 ← depends on Application (via ICommandDispatcher / IQueryDispatcher), Contracts
Mcp                 ← depends on Application (dispatchers), Shared.Infrastructure.Web; never Api
Contracts           ← depends on Shared.Abstractions.Messaging only (for IIntegrationEvent on integration-event records)

Cross-module (OK):    ModuleA.Application → ModuleB.Contracts
Cross-module (NEVER): ModuleA.Application → ModuleB.Domain
Cross-module (NEVER): ModuleA.Application → ModuleB.Infrastructure
Cross-module (NEVER): ModuleA.Infrastructure → ModuleB.Infrastructure (shared DbContext)
```

## Module Registration

Each module exposes exactly one public extension method. The host project calls them — modules
do not self-register via assembly scanning.

```csharp
// BudgetPlan.Api/DependencyInjection.cs
public static class BudgetPlanModule
{
    public static IServiceCollection AddBudgetPlanModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddBudgetPlanApplication()
            .AddBudgetPlanInfrastructure(configuration);

        return services;
    }
}

// Host Api/Program.cs
builder.Services
    .AddBudgetPlanModule(builder.Configuration)
    .AddUserPreferencesModule(builder.Configuration)
    .AddNotificationsModule(builder.Configuration);

// After every module: register the shared CQRS dispatcher chain exactly once.
builder.Services.AddCqrsDispatchers();
```

**Multiple Style-1 (EF) modules.** The CQRS dispatcher chain and the integration-event bus
are host-level singletons shared by every module:

- A module's infrastructure DI calls `AddCqrsHandlers(...)` (handler scan only) — never a
  per-module dispatcher registration.
- `AddDomainEventDispatcher()` is idempotent; call it from every Style-1 module.
- The **first** Style-1 module owns the global `IUnitOfWork` binding. A second+ Style-1
  module must expose a **module-scoped** unit-of-work abstraction
  (`I{Module}UnitOfWork : IUnitOfWork`, bound to its own `DbContext`) and have its handlers
  depend on that — exactly as `Notifications` does with `INotificationsUnitOfWork`.
- `AddOutbox<TDbContext>()` is multi-publisher safe: each module keeps its own keyed store,
  and the bus routes to the right one via `OutboxScope` during domain-event dispatch.
