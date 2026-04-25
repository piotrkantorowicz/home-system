# Backend — C# Coding Standards

## Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10, C# 13 |
| API | ASP.NET Core — Minimal API endpoints |
| ORM | Entity Framework Core 10 + Npgsql (PostgreSQL) |
| Messaging | RabbitMQ via `RabbitMQ.Client` (no MassTransit) |
| Auth | OIDC/OAuth2 (token-based, no session state) |
| CQRS | Custom `ICommandDispatcher` / `IQueryDispatcher` (no MediatR) |
| Validation | Custom `ICommandValidator<TCommand>` — called by `ValidationCommandDispatcherDecorator` |
| Testing | xUnit + Shouldly + NSubstitute |
| Packages | Central Package Management — versions in `Directory.Packages.props` only |

## NuGet Rules

- **Never** put version numbers in `.csproj` files — always `Directory.Packages.props`.
- Use `<PackageReference Include="..." />` without version in `.csproj`.
- Pin every package version explicitly — no floating ranges (`*`, `1.0.*`).

## .NET Project Conventions

```xml
<!-- Every project has these in Directory.Build.props -->
<PropertyGroup>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <AnalysisMode>All</AnalysisMode>
  <LangVersion>latest</LangVersion>
</PropertyGroup>
```

## Naming

| Element | Convention | Example |
|---|---|---|
| Types, Methods, Properties | PascalCase | `BudgetPlan`, `AddEntry()`, `TotalAmount` |
| Local variables, parameters | camelCase | `budgetPlan`, `userId` |
| Private fields | `_camelCase` | `_repository`, `_unitOfWork` |
| Constants | PascalCase | `MaxEntriesPerPlan` |
| Interfaces | `I` prefix | `IBudgetPlanRepository` |
| Async methods | `Async` suffix | `GetByIdAsync()`, `CommitAsync()` |
| CancellationToken parameter | always `ct` | `async Task FooAsync(CancellationToken ct)` |
| Generic type parameters | `T` or descriptive | `TEntity`, `TResult` |
| Test methods | `Method_State_Expected` | `AddEntry_WhenLimitExceeded_ThrowsDomainException` |

## File & Namespace Rules

- **One type per file.** File name must equal the type name exactly.
- **Namespace = folder path** from the project root. No exceptions.
- `using` directives sorted: `System.*` → `Microsoft.*` → third-party → own project.
- No `using static` outside of test files.

## Nullability

- Nullable reference types are **always enabled** (`<Nullable>enable</Nullable>`).
- Never use the null-forgiving operator (`!`) unless you can prove the value is non-null at that point.
- Use `ArgumentNullException.ThrowIfNull(param)` at the top of public constructors and methods.
- Return `T?` (nullable) when absence is a valid, expected case. Return `T` when null would be a bug.

## Records vs. Classes

| Use `record` for | Use `class` for |
|---|---|
| Value Objects | Aggregate Roots |
| Typed IDs | Entities |
| DTOs (query results, request/response) | Domain Services |
| Integration Events | Application Services |
| Domain Events | Repositories / Handlers |

## `var` Usage

```csharp
var plan = new BudgetPlan();            // ✅ obvious
var id = BudgetPlanId.New();           // ✅ obvious
var result = await GetAsync();         // ❌ — what does Get return? Use explicit type
```

Use explicit types for method return values that aren't obvious, collections, and LINQ results.

## Sealed by Default

```csharp
// ✅ Correct — sealed unless inheritance is explicitly designed for
public sealed class CreateBudgetPlanCommandHandler : ICommandHandler<CreateBudgetPlanCommand> { }

// ❌ Wrong — leaves it open for accidental inheritance
public class CreateBudgetPlanCommandHandler : ICommandHandler<CreateBudgetPlanCommand> { }
```

## async / await

- Every I/O operation must be `async` — no `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.
- `CancellationToken ct` must be the **last parameter** on every async method.
- Pass `ct` down to every I/O call — never swallow or ignore it.
- Never use `async void` except for event handlers (and even then, prefer a sync wrapper).

## Exception Handling

```csharp
// Domain rule violations
throw new DomainException("Budget limit would be exceeded.");

// Expected not-found (preferred) — use Result type, not exception
return Result.Failure<BudgetPlanDto>("Budget plan not found.");

// Infrastructure errors — let them propagate, catch at boundary (middleware)
// Never catch Exception without re-throwing or structured logging
```

- **Never swallow exceptions** with an empty `catch { }`.
- **Never catch `Exception`** at domain or application layer. Catch specific types only.
- Domain exceptions carry a message in business language, not technical detail.

## Collections

```csharp
// Read-only exposure of internal collections
private readonly List<BudgetEntry> _entries = [];
public IReadOnlyCollection<BudgetEntry> Entries => _entries.AsReadOnly();

// Prefer collection expressions (C# 12+)
var ids = [id1, id2];                      // ✅ collection expression

// Prefer LINQ for readable transformations, but avoid chaining > 4 methods
// Extract to a named method instead
```

## Immutability

- Value objects and DTOs: **always immutable** — use `init` or constructor-only assignment.
- Aggregate root state: mutated only via methods, exposed via `{ get; private set; }`.
- Never use `ref` or `out` parameters in domain or application code.

## Things That Are Banned

```
❌ dynamic types
❌ AutoMapper — map manually; a static Map() method or extension is fine
❌ MediatR — use custom dispatcher stack
❌ MassTransit — use raw RabbitMQ.Client
❌ Magic strings / numbers — use named constants or configuration
❌ String-keyed queues/topics — use typed constants
❌ Public setters on aggregate root or entity properties
❌ .Result / .Wait() on Tasks
❌ Returning domain entities from Application layer
❌ [Obsolete] code — remove it immediately
❌ Regions (#region) — if you need regions, the class is too large; split it
```
