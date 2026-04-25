Add a new aggregate root (and related files) to an existing DDD module.

## Usage

```
/scaffold-aggregate <ModuleName> <AggregateName>
```

Example: `/scaffold-aggregate BudgetPlan SpendingGoal`

## Instructions

Read `docs/rules/backend-ddd-patterns.md` before generating anything.

Given `ModuleName` and `AggregateName`, generate:

**Domain layer** (`{ModuleName}.Domain/`):
- `Aggregates/{AggregateName}.cs`
  - Private parameterless constructor (EF Core)
  - Static `Create()` factory method
  - Typed ID property using `{AggregateName}Id`
  - `RaiseDomainEvent` call in `Create()`
  - All properties `{ get; private set; }`
- `ValueObjects/{AggregateName}Id.cs`
  - `sealed record` with `Guid Value`
  - `static New()` and `static From(Guid)` methods
- `Events/{AggregateName}CreatedDomainEvent.cs`
  - `sealed record` implementing `IDomainEvent`
- `Aggregates/{AggregateName}Exceptions.cs`
  - `sealed class` extending `DomainException`
- `Repositories/I{AggregateName}Repository.cs`
  - `GetByIdAsync`, `AddAsync`, `Update`, `Delete`

**Infrastructure layer** (`{ModuleName}.Infrastructure/`):
- `Persistence/Configurations/{AggregateName}Configuration.cs`
  - Table name in `snake_case`
  - Typed ID conversion
  - Value object owned types
- `Persistence/Repositories/{AggregateName}Repository.cs`
  - Internal sealed, implements the interface

**After generating:**
- Register the repository in `{ModuleName}.Infrastructure/DependencyInjection.cs`
- Add `DbSet<{AggregateName}>` to the module's `DbContext`
- Remind the user to create a migration
