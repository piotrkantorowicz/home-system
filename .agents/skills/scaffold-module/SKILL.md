---
name: scaffold-module
description: "Scaffold a complete new module following the project's architecture conventions"
---

# scaffold-module

Scaffold a complete new module following the project's architecture conventions.

## Usage

```
$scaffold-module <ModuleName> [--crud | --ddd]
```

- `--ddd` (default): Full DDD module with Domain, Application, Infrastructure, Contracts, Api projects
- `--crud`: Lightweight CRUD module with Application, Infrastructure, Api projects only

## Instructions

Given the module name and mode provided by the user:

1. **Read** `docs/rules/backend-module-structure.md` to confirm the expected folder layout.
2. **Read** `docs/rules/backend-ddd-patterns.md` if mode is `--ddd`.
3. **Read** `docs/rules/backend-cqrs-patterns.md` for handler scaffolding.
4. **Read** `docs/rules/backend-ef-core-patterns.md` for DbContext and configuration scaffolding.

Then generate all of the following, adapting names for the given module:

### For `--ddd` mode, generate:

**`{ModuleName}.Domain/`**
- `Aggregates/{ModuleName}.cs` — with private constructor, static `Create()`, typed ID, `RaiseDomainEvent`
- `Aggregates/{ModuleName}Exceptions.cs` — module-specific domain exception class
- `ValueObjects/{ModuleName}Id.cs` — typed ID record with `New()` and `From(Guid)`
- `Events/{ModuleName}CreatedDomainEvent.cs` — sealed record implementing `IDomainEvent`
- `Repositories/I{ModuleName}Repository.cs` — interface with standard CRUD methods

**`{ModuleName}.Application/`**
- `Commands/Create{ModuleName}/Create{ModuleName}Command.cs` — record implementing `ICommand<Guid>`
- `Commands/Create{ModuleName}/Create{ModuleName}CommandHandler.cs` — internal sealed handler
- `Commands/Create{ModuleName}/Create{ModuleName}CommandValidator.cs` — input validation
- `Queries/Get{ModuleName}/Get{ModuleName}Query.cs` — record implementing `IQuery<{ModuleName}Dto?>`
- `Queries/Get{ModuleName}/Get{ModuleName}QueryHandler.cs` — uses `AsNoTracking()` + `Select()`
- `Queries/Get{ModuleName}/{ModuleName}Dto.cs` — sealed record DTO
- `EventHandlers/{ModuleName}CreatedDomainEventHandler.cs` — maps domain event → outbox

**`{ModuleName}.Contracts/`**
- `Events/{ModuleName}CreatedIntegrationEvent.cs` — sealed record with primitives only

**`{ModuleName}.Infrastructure/`**
- `Persistence/{ModuleName}DbContext.cs` — with `IUnitOfWork` implementation
- `Persistence/Configurations/{ModuleName}Configuration.cs` — `IEntityTypeConfiguration<T>`
- `Persistence/Repositories/{ModuleName}Repository.cs` — concrete implementation
- `Persistence/Migrations/` — empty folder
- `DependencyInjection.cs` — internal wiring

**`{ModuleName}.Api/`**
- `{ModuleName}Endpoints.cs` — MapGroup with Create + Get endpoints
- `DependencyInjection.cs` — public `Add{ModuleName}Module()` extension method

### For `--crud` mode, generate:

Same as above but **without** the Domain project and Contracts project. No aggregates, no domain events, no repository interface — the handler works directly with `DbContext`.

### After generating:

1. Add all new `.csproj` projects to the solution file.
2. Add project references respecting layer dependencies.
3. Show the user what to add to `Api/Program.cs`:
   ```csharp
   builder.Services.Add{ModuleName}Module(builder.Configuration);
   app.Map{ModuleName}Endpoints();
   ```
