---
name: scaffold-endpoint
description: "Add a new API endpoint along with its command or query to an existing module"
---

# scaffold-endpoint

Add a new API endpoint along with its command or query to an existing module.

## Usage

```
$scaffold-endpoint <ModuleName> <AggregateName> <Operation> [--command | --query]
```

Examples:
```
$scaffold-endpoint BudgetPlan BudgetPlan Close --command
$scaffold-endpoint BudgetPlan BudgetPlan ListByUser --query
```

## Instructions

Read `.claude/rules/backend-cqrs-patterns.md` and `.claude/rules/backend-api-patterns.md` before generating.

### For `--command`:

Generate:
- `{ModuleName}.Application/Commands/{Operation}{AggregateName}/{Operation}{AggregateName}Command.cs`
- `{ModuleName}.Application/Commands/{Operation}{AggregateName}/{Operation}{AggregateName}CommandHandler.cs`
  - Loads aggregate via repository
  - Calls the appropriate aggregate method
  - Calls `_unitOfWork.CommitAsync(ct)`
- `{ModuleName}.Application/Commands/{Operation}{AggregateName}/{Operation}{AggregateName}CommandValidator.cs`

Add to endpoints class:
- A new endpoint method using `ICommandDispatcher`
- Correct HTTP verb (POST for create, PUT/PATCH for update, DELETE for delete)
- Correct status code (201 for create, 204 for update/delete)

### For `--query`:

Generate:
- `{ModuleName}.Application/Queries/{Operation}{AggregateName}/{Operation}{AggregateName}Query.cs`
- `{ModuleName}.Application/Queries/{Operation}{AggregateName}/{Operation}{AggregateName}QueryHandler.cs`
  - Uses `DbContext` directly with `AsNoTracking()` + `Select()`
  - Never loads a full aggregate
- `{ModuleName}.Application/Queries/{Operation}{AggregateName}/{AggregateName}Dto.cs` (if not existing)

Add to endpoints class:
- A new GET endpoint using `IQueryDispatcher`
- If list endpoint: use `PagedList<TDto>` and `[AsParameters]` for query params

### After generating:
- Ensure the handler is registered (assembly scanning handles this automatically via `AddCqrs`)
- Add OpenAPI metadata (`.WithName()`, `.WithSummary()`, `.Produces<>()`)
