# Backend — C# Coding Standards

## Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10, C# 14 |
| API | ASP.NET Core Minimal APIs, typed results, built-in OpenAPI (`AddOpenApi`) + Scalar UI |
| ORM | EF Core 10 + Npgsql (Style 1) / Dapper + Npgsql (Style 2) — see `backend-persistence-styles.md` |
| Messaging | Custom outbox/inbox bus, in-process transport (RabbitMQ transport planned, `#168`) |
| Auth | OIDC / JWT bearer from Authentik, no session state |
| CQRS | Custom `ICommandDispatcher` / `IQueryDispatcher` (no MediatR) |
| Validation | Custom `ICommandValidator<TCommand>`, run by `ValidationCommandDispatcherDecorator` |
| Time | `TimeProvider` (`TimeProvider.System` in DI, `FakeTimeProvider` in tests) |
| Testing | xUnit + Shouldly + NSubstitute + Testcontainers |
| Packages | Central Package Management — versions in `Directory.Packages.props` only |

## Build settings (`Directory.Build.props`)

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>false</TreatWarningsAsErrors>   <!-- flipping to true is tracked in #269 -->
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```

Style rules live in the root `.editorconfig`. `dotnet format HomeSystem.slnx --verify-no-changes`
is a CI gate and part of `scripts/verify.sh` — if the build warns about IDE/CA rules, fix the
code, do not lower the severity.

## NuGet Rules

- **Never** put version numbers in `.csproj` files — always `Directory.Packages.props`.
- `<PackageReference Include="..." />` without version in `.csproj`.
- Pin every version explicitly — no floating ranges.
- A package nothing references gets removed from `Directory.Packages.props`.

## Naming

| Element | Convention | Example |
|---|---|---|
| Types, methods, properties | PascalCase | `BudgetPlan`, `AddEntry()`, `TotalAmount` |
| Local variables, parameters | camelCase — never `_`-prefixed | `budgetPlan`, `userId` |
| Private fields | `_camelCase` | `_repository`, `_unitOfWork` |
| Constants, static readonly | PascalCase | `MaxEntriesPerPlan` |
| Interfaces | `I` prefix | `IBudgetPlanRepository` |
| Async methods | `Async` suffix | `GetByIdAsync()`, `CommitAsync()` |
| Endpoint handler methods | **no** `Async` suffix — the name feeds `.WithName()` / OpenAPI operationId | `GetProfile`, `CreateHousehold` |
| Test methods | `Method_State_Expected`, no `Async` suffix | `AddEntry_WhenLimitExceeded_ThrowsDomainException` |
| CancellationToken parameter | always `ct`, always last | `Task FooAsync(Guid id, CancellationToken ct)` |
| Generic type parameters | `T` or descriptive | `TEntity`, `TResult` |

## File & Namespace Rules

- **One type per file.** File name equals the type name.
- **Namespace = folder path** from the project root. File-scoped namespaces only.
- `using` directives go **inside** the file-scoped namespace, `System.*` first, then alphabetical.
  `dotnet format` enforces both.
- No `using static` outside test files. No `#region`.

```csharp
namespace Household.Application.Commands.RenameHousehold;

using Household.Domain.Repositories;
using Shared.Abstractions.Cqrs;

internal sealed class RenameHouseholdCommandHandler(...) : ICommandHandler<RenameHouseholdCommand> { }
```

## Constructors — primary constructors for services

Handlers, repositories, services, decorators, workers: **primary constructors** (C# 12+).
Captured parameters are used directly; no `_field = param` boilerplate, no tuple assignment.

```csharp
// ✅
internal sealed class CreateHouseholdCommandHandler(
    IHouseholdRepository households,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreateHouseholdCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateHouseholdCommand command, CancellationToken ct)
    {
        var household = Household.Create(HouseholdId.New(), command.Name, clock.GetUtcNow());
        await households.AddAsync(household, ct);
        await unitOfWork.CommitAsync(ct);
        return household.Id.Value;
    }
}

// ❌ legacy — do not write new code like this; migrate opportunistically when touching a file
internal sealed class CreateHouseholdCommandHandler : ICommandHandler<CreateHouseholdCommand, Guid>
{
    private readonly IHouseholdRepository _households;
    private readonly IHouseholdUnitOfWork _unitOfWork;
    public CreateHouseholdCommandHandler(IHouseholdRepository households, IHouseholdUnitOfWork unitOfWork)
        => (_households, _unitOfWork) = (households, unitOfWork);
}
```

**Exceptions:** aggregates and entities keep an explicit `private` parameterless constructor
(EF Core / Dapper materialisation) and a static `Create()` factory — see `backend-ddd-patterns.md`.
Classes whose constructor does real work (validation, `ArgumentNullException.ThrowIfNull`
on a nullable-disabled boundary) may keep an explicit constructor.

## Time — `TimeProvider`, never `DateTime.UtcNow`

```csharp
// DI (host): builder.Services.AddSingleton(TimeProvider.System);
// Handler / service:
internal sealed class WaterReminderJob(TimeProvider clock, ...)
{
    public Task RunAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();          // DateTimeOffset
        ...
    }
}

// Aggregate: time is passed in, never read from the ambient clock
public static Household Create(HouseholdId id, string name, DateTimeOffset now) { ... }
```

- Application / infrastructure code injects `TimeProvider`.
- Domain code takes the timestamp as a parameter (aggregates have no DI).
- Tests use `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) — no `Task.Delay`
  polling, no "today" arithmetic that breaks at midnight.
- Prefer `DateTimeOffset` at the edges; `DateTime` (UTC) only where the schema already stores it.
- Existing `DateTime.UtcNow` call sites are being migrated under #269;
  do not add new ones.

## Identifiers — `Guid.CreateVersion7()`

Typed IDs generate **version-7 GUIDs** (.NET 9+): time-ordered, so PostgreSQL b-tree
indexes append instead of splitting.

```csharp
public sealed record HouseholdId(Guid Value)
{
    public static HouseholdId New() => new(Guid.CreateVersion7());
    public static HouseholdId From(Guid value) => new(value);
}
```

`Guid.NewGuid()` only where ordering is irrelevant and the value must be unpredictable
(tokens, nonces).

## Nullability

- Nullable reference types are always on.
- Never use the null-forgiving operator (`!`) unless the non-null-ness is provable at that line;
  add a comment saying why.
- `ArgumentNullException.ThrowIfNull(param)` / `ArgumentException.ThrowIfNullOrWhiteSpace(s)`
  at the top of **public** methods on domain types. Not needed on internal handlers whose inputs
  are DI-resolved.
- Return `T?` when absence is a valid, expected case. Return `T` when null would be a bug.
- EF-materialised non-nullable properties: `= default!;` on the property with the private
  constructor — the only sanctioned `!`.
- `required` members for DTO-like classes that are object-initialised (outbox entities, options).

## Records vs. Classes

| Use `record` for | Use `class` for |
|---|---|
| Value objects, typed IDs | Aggregate roots, entities |
| DTOs (query results, request/response) | Domain services, application services |
| Integration events, domain events | Repositories, handlers, workers |
| Commands, queries | Options classes (mutable binding) |

## `var`

```csharp
var plan = new BudgetPlan();                 // ✅ type apparent
var id = BudgetPlanId.New();                 // ✅ type apparent
var result = await GetAsync();               // ❌ — what does Get return? Use the explicit type
IReadOnlyList<MealDto> meals = await ...;    // ✅
```

Explicit types for: method return values that aren't obvious from the call, collections, LINQ
results, numeric literals. `.editorconfig` encodes this as `var_when_type_is_apparent`.

## Sealed by Default

Every class is `sealed` unless inheritance is explicitly designed for. Applies to handlers,
services, DTO classes, test classes. CA1852 warns on unsealed internal types.

## Collections

```csharp
private readonly List<BudgetEntry> _entries = [];                       // ✅ collection expression
public IReadOnlyCollection<BudgetEntry> Entries => _entries.AsReadOnly();

Guid[] ids = [id1, id2];                                                 // ✅ target-typed
IReadOnlyList<Guid> ids = [id1, id2];                                    // ✅
var ids = [id1, id2];                                                    // ❌ does not compile — no target type
```

- Expose `IReadOnlyCollection<T>` / `IReadOnlyList<T>`, never `List<T>`, from public surface.
- Return `[]` not `Array.Empty<T>()` / `new List<T>()`.
- LINQ chains longer than ~4 calls → extract a named method.

## async / await

- Every I/O method is `async` — no `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` (CA1849 warns).
- `CancellationToken ct` is the **last** parameter and is passed to every I/O call (CA2016 warns).
- `ConfigureAwait(false)` **only in `Shared.*` library projects.** Application, module and host
  code never write it — there is no synchronisation context in ASP.NET Core and it only adds noise.
- Never `async void`.
- Hosted-service loops (`OutboxWorker`, tick services) catch broadly **at the loop boundary
  only**, always excluding cancellation: `catch (Exception ex) when (ex is not OperationCanceledException)`.

## Exception Handling

```csharp
throw new HouseholdDomainException("A household must keep at least one owner.");   // domain rule
throw new NotFoundException($"Household {id} was not found.");                     // → 404
throw new ForbiddenException("Only an owner can rename the household.");           // → 403
```

- Exceptions from `Shared.Abstractions.Core` map to HTTP in one place (see
  `backend-api-patterns.md`). Endpoints and handlers never build `ProblemDetails`.
- Never swallow exceptions. Never catch `Exception` in domain or application code.
- Domain exceptions carry business language, no stack traces, no SQL detail.

## Immutability

- Value objects and DTOs: immutable (`record`, `init`).
- Aggregate state: `{ get; private set; }`, mutated only through named methods.
- No `ref` / `out` in domain or application code (`TryGetValue` in local helpers is fine).

## Logging

- Structured logging with named placeholders: `_logger.LogError(ex, "Failed to dispatch {MessageId}", id)`.
- Hot paths (workers, transports): `[LoggerMessage]` source-generated methods.
- Never log tokens, emails, or full request bodies.

## Things That Are Banned

```
❌ dynamic
❌ AutoMapper / MediatR / MassTransit / FluentValidation
❌ DateTime.UtcNow / DateTime.Now in new code — inject TimeProvider
❌ Guid.NewGuid() for entity IDs — Guid.CreateVersion7()
❌ Magic strings / numbers — named constants or options
❌ Public setters on aggregate root or entity properties
❌ .Result / .Wait() on Tasks
❌ ConfigureAwait(false) outside Shared.*
❌ Returning domain entities from the Application layer
❌ [Obsolete] code — delete it
❌ #region
❌ Lowering an analyzer severity to make a build green
```
