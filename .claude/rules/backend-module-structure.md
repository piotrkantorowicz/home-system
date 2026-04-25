# 02 — Module Structure

## Solution Layout

```
src/
  Modules/
    {ModuleName}/
      {ModuleName}.Api/
      {ModuleName}.Application/
      {ModuleName}.Contracts/
      {ModuleName}.Domain/
      {ModuleName}.Infrastructure/
  Shared/
    Shared.Abstractions/          # Interfaces only: IDomainEvent, IIntegrationEvent,
                                  # IUnitOfWork, ICommand, IQuery<T>, etc.
    Shared.Infrastructure/        # Cross-cutting implementations: clock, outbox worker,
                                  # MediatR pipeline behaviours, base DbContext helpers
  Api/                            # Host project — wires modules, no business logic

tests/
  {ModuleName}.UnitTests/
  {ModuleName}.IntegrationTests/
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
```

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
Domain              ← depends on nothing
Application         ← depends on Domain, Shared.Abstractions
Infrastructure      ← depends on Domain, Application (for interfaces), EF Core, RabbitMQ.Client
Api                 ← depends on Application (via MediatR), Contracts
Contracts           ← depends on nothing (plain C# records/interfaces only)

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
```
