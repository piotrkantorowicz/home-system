# Shared Messaging (Integration-Event Bus + Outbox/Inbox + In-Process Transport) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the shared messaging infrastructure (integration-event bus, outbox/inbox, transport seam, in-process v1 transport, EF + Dapper inbox executors) so subsequent issues (N3 Notifications skeleton, N6 DietPlanner outbox) can publish and consume integration events without further reshuffling.

**Architecture:** A small set of contracts in `Shared.Abstractions.Messaging` is implemented across three new infrastructure projects. `Shared.Infrastructure.Messaging` (core) is persistence-agnostic and hosts the bus, the worker, and the transport seam. `Shared.Infrastructure.Messaging.Ef` provides EF-backed outbox + inbox stores parameterised on the consuming module's `DbContext`. `Shared.Infrastructure.Messaging.Dapper` provides Dapper-backed outbox + inbox stores parameterised on a per-module `INpgsqlConnectionFactory`. The v1 transport is in-process; the seam exists so a RabbitMQ implementation drops in later (N18).

**Tech Stack:** .NET 10, EF Core 10, Npgsql 9, Dapper 2.1, xUnit + Shouldly + NSubstitute + FluentAssertions, Testcontainers.PostgreSql.

**Issue:** [#152](https://github.com/piotrkantorowicz/home-system/issues/152) (N2 in spec).

**Reference:** `docs/superpowers/specs/2026-04-25-notifications-module-design.md` §6.

---

## Parallelism

Tasks 1, 2, 3 are foundational and must run sequentially. After Task 3, **Tasks 4, 5, 6 can run in parallel** in three subagents (one per project: core / EF / Dapper) — they only share the Task 3 abstractions, which are stable contracts. Task 7 (integration tests) depends on all three. Task 8 (cleanup) is last.

```text
Task 1 (deps) ──► Task 2 (projects) ──► Task 3 (abstractions)
                                            │
                              ┌─────────────┼─────────────┐
                              ▼             ▼             ▼
                          Task 4        Task 5         Task 6
                          (core)        (EF)          (Dapper)
                              └─────────────┼─────────────┘
                                            ▼
                                       Task 7 (integration tests)
                                            ▼
                                       Task 8 (host wiring + cleanup)
```

---

## File structure

**New projects (one type per file unless trivially small):**

```
src/Shared/
  Shared.Abstractions.Messaging/
    IIntegrationEvent.cs                 # MODIFY — add EventId, OccurredAt
    IIntegrationEventBus.cs              # CREATE
    IIntegrationEventHandler.cs          # CREATE
    IInboxExecutor.cs                    # CREATE

  Shared.Infrastructure.Messaging/
    Shared.Infrastructure.Messaging.csproj
    Outbox/
      OutboxMessage.cs                   # CREATE — POCO, no EF
      IOutboxStore.cs                    # CREATE
      OutboxIntegrationEventBus.cs       # CREATE — implements IIntegrationEventBus
      OutboxWorker.cs                    # CREATE — BackgroundService
      OutboxWorkerOptions.cs             # CREATE — IOptions
    Transport/
      IIntegrationEventTransport.cs      # CREATE
      InProcessIntegrationEventTransport.cs  # CREATE
    Extensions/
      MessagingBuilder.cs                # CREATE — fluent DI builder
      MessagingExtensions.cs             # CREATE — AddIntegrationEventBus()
    Serialization/
      IntegrationEventSerializer.cs      # CREATE — System.Text.Json wrapper

  Shared.Infrastructure.Messaging.Ef/
    Shared.Infrastructure.Messaging.Ef.csproj
    Outbox/
      EfOutboxStore.cs                   # CREATE — IOutboxStore<TDbContext>
      OutboxMessageEntity.cs             # CREATE — EF entity (table-mapped)
      OutboxMessageEntityConfiguration.cs # CREATE — IEntityTypeConfiguration
    Inbox/
      EfInboxExecutor.cs                 # CREATE — IInboxExecutor
      InboxMessageEntity.cs              # CREATE
      InboxMessageEntityConfiguration.cs # CREATE
    Extensions/
      EfMessagingExtensions.cs           # CREATE — AddOutbox<TDbContext>(), AddInbox<TDbContext>()

  Shared.Infrastructure.Messaging.Dapper/
    Shared.Infrastructure.Messaging.Dapper.csproj
    INpgsqlConnectionFactory.cs          # CREATE — abstraction modules implement
    Inbox/
      DapperInboxExecutor.cs             # CREATE
      InboxSql.cs                        # CREATE — SQL constants
    Extensions/
      DapperMessagingExtensions.cs       # CREATE — AddDapperInbox<TFactory>()

  Shared.Messaging.Tests/                # CREATE — unit-test project
    Shared.Messaging.Tests.csproj
    Outbox/
      OutboxIntegrationEventBusTests.cs
      OutboxWorkerTests.cs
    Transport/
      InProcessIntegrationEventTransportTests.cs
    Serialization/
      IntegrationEventSerializerTests.cs

  Shared.Messaging.IntegrationTests/     # CREATE — integration tests with Testcontainers
    Shared.Messaging.IntegrationTests.csproj
    Fixtures/
      PostgresContainerFixture.cs
      MessagingTestDbContext.cs
    Ef/
      EfInboxExecutorIntegrationTests.cs
      EfOutboxStoreIntegrationTests.cs
    Dapper/
      DapperInboxExecutorIntegrationTests.cs
      TestNpgsqlConnectionFactory.cs
    Roundtrip/
      InProcessRoundtripIntegrationTests.cs
```

**Modified files:**

```
HomeSystem.slnx                           # Add 5 new projects
Directory.Packages.props                  # Add Dapper, Npgsql packages
CLAUDE.md                                 # Add new projects to repo-structure block
```

**Why this split**

- **Abstractions are tiny.** Splitting them out keeps the contract surface visible.
- **Core has no EF / Dapper / Npgsql dependency.** A future RabbitMQ transport drops in next to `InProcessIntegrationEventTransport` without affecting persistence.
- **EF and Dapper variants are separate projects** so a Dapper module never transitively pulls EF Core, and vice versa. This was identified as a goal in the N0 split (spec §5.2).

---

## Conventions all tasks follow

- **Coding standards:** `.claude/rules/backend-coding-standards.md` (sealed by default, `ct` parameter, no public setters, etc).
- **Tests:** xUnit + Shouldly. Mocks via NSubstitute. File-per-test-class.
- **Test naming:** `Method_State_Expected`.
- **Each task ends with**: `dotnet build HomeSystem.slnx` green, all new tests passing, conventional commit referencing `#152`.
- **Commit prefix:** `feat(shared)` for production code, `test(shared)` for test-only commits.
- **Do not add** `Co-Authored-By: Claude` trailers (per CLAUDE.md).

---

## Task 1: Add Dapper and Npgsql to central package management

**Files:**
- Modify: `Directory.Packages.props`

- [ ] **Step 1: Open `Directory.Packages.props` and add three package versions**

Add inside the existing `<ItemGroup>` after the EF Core block:

```xml
    <!-- Dapper / Npgsql (Style-2 modules + messaging Dapper inbox executor) -->
    <PackageVersion Include="Dapper" Version="2.1.66" />
    <PackageVersion Include="Npgsql" Version="9.0.2" />

    <!-- Hosting (BackgroundService for OutboxWorker) -->
    <PackageVersion Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.1" />
    <PackageVersion Include="Microsoft.Extensions.Options" Version="10.0.1" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.1" />
```

- [ ] **Step 2: Verify**

Run: `dotnet restore HomeSystem.slnx`
Expected: succeeds, no warnings about the new versions.

- [ ] **Step 3: Commit**

```bash
git add Directory.Packages.props
git commit -m "chore(shared): add Dapper, Npgsql, hosting packages for messaging (#152)"
```

---

## Task 2: Create empty projects and wire them into the solution

**Files (all new):**
- Create: `src/Shared/Shared.Infrastructure.Messaging/Shared.Infrastructure.Messaging.csproj`
- Create: `src/Shared/Shared.Infrastructure.Messaging.Ef/Shared.Infrastructure.Messaging.Ef.csproj`
- Create: `src/Shared/Shared.Infrastructure.Messaging.Dapper/Shared.Infrastructure.Messaging.Dapper.csproj`
- Create: `src/Shared/Shared.Messaging.Tests/Shared.Messaging.Tests.csproj`
- Create: `src/Shared/Shared.Messaging.IntegrationTests/Shared.Messaging.IntegrationTests.csproj`
- Modify: `HomeSystem.slnx`

- [ ] **Step 1: Create `Shared.Infrastructure.Messaging.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Options" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Shared.Abstractions.Core\Shared.Abstractions.Core.csproj" />
    <ProjectReference Include="..\Shared.Abstractions.Messaging\Shared.Abstractions.Messaging.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create `Shared.Infrastructure.Messaging.Ef.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Shared.Abstractions.Messaging\Shared.Abstractions.Messaging.csproj" />
    <ProjectReference Include="..\Shared.Infrastructure.Messaging\Shared.Infrastructure.Messaging.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create `Shared.Infrastructure.Messaging.Dapper.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Dapper" />
    <PackageReference Include="Npgsql" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Shared.Abstractions.Messaging\Shared.Abstractions.Messaging.csproj" />
    <ProjectReference Include="..\Shared.Infrastructure.Messaging\Shared.Infrastructure.Messaging.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Create `Shared.Messaging.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Shared.Abstractions.Messaging\Shared.Abstractions.Messaging.csproj" />
    <ProjectReference Include="..\Shared.Infrastructure.Messaging\Shared.Infrastructure.Messaging.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 5: Create `Shared.Messaging.IntegrationTests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Testcontainers.PostgreSql" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
    <PackageReference Include="Dapper" />
    <PackageReference Include="Npgsql" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Shared.Abstractions.Messaging\Shared.Abstractions.Messaging.csproj" />
    <ProjectReference Include="..\Shared.Infrastructure.Messaging\Shared.Infrastructure.Messaging.csproj" />
    <ProjectReference Include="..\Shared.Infrastructure.Messaging.Ef\Shared.Infrastructure.Messaging.Ef.csproj" />
    <ProjectReference Include="..\Shared.Infrastructure.Messaging.Dapper\Shared.Infrastructure.Messaging.Dapper.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 6: Add the five projects to `HomeSystem.slnx`**

In the `<Folder Name="/Shared/">` block, insert:

```xml
    <Project Path="src/Shared/Shared.Infrastructure.Messaging/Shared.Infrastructure.Messaging.csproj" />
    <Project Path="src/Shared/Shared.Infrastructure.Messaging.Ef/Shared.Infrastructure.Messaging.Ef.csproj" />
    <Project Path="src/Shared/Shared.Infrastructure.Messaging.Dapper/Shared.Infrastructure.Messaging.Dapper.csproj" />
```

In the `<Folder Name="/Tests/">` block, append:

```xml
    <Project Path="src/Shared/Shared.Messaging.Tests/Shared.Messaging.Tests.csproj" />
    <Project Path="src/Shared/Shared.Messaging.IntegrationTests/Shared.Messaging.IntegrationTests.csproj" />
```

- [ ] **Step 7: Build to confirm empty projects compile**

Run: `dotnet build HomeSystem.slnx`
Expected: build succeeds. The new projects are empty so there is nothing to compile in them yet — that's fine.

- [ ] **Step 8: Commit**

```bash
git add HomeSystem.slnx src/Shared/Shared.Infrastructure.Messaging \
        src/Shared/Shared.Infrastructure.Messaging.Ef \
        src/Shared/Shared.Infrastructure.Messaging.Dapper \
        src/Shared/Shared.Messaging.Tests \
        src/Shared/Shared.Messaging.IntegrationTests
git commit -m "feat(shared): scaffold messaging infra + test projects (#152)"
```

---

## Task 3: Add abstractions to `Shared.Abstractions.Messaging`

**Files:**
- Modify: `src/Shared/Shared.Abstractions.Messaging/IIntegrationEvent.cs`
- Create: `src/Shared/Shared.Abstractions.Messaging/IIntegrationEventBus.cs`
- Create: `src/Shared/Shared.Abstractions.Messaging/IIntegrationEventHandler.cs`
- Create: `src/Shared/Shared.Abstractions.Messaging/IInboxExecutor.cs`

- [ ] **Step 1: Update `IIntegrationEvent.cs`**

```csharp
namespace Shared.Abstractions.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
```

- [ ] **Step 2: Create `IIntegrationEventBus.cs`**

```csharp
namespace Shared.Abstractions.Messaging;

public interface IIntegrationEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
```

- [ ] **Step 3: Create `IIntegrationEventHandler.cs`**

```csharp
namespace Shared.Abstractions.Messaging;

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create `IInboxExecutor.cs`**

The spec marks this `internal`, but it must be `public` because it crosses assembly boundaries (core dispatches into module-owned executors). Use `public` and document the constraint via XML doc.

```csharp
namespace Shared.Abstractions.Messaging;

/// <summary>
/// Idempotent execution boundary for an integration-event handler in a consuming module.
/// Implementations open a transaction on the consumer's storage, check the inbox for the
/// given <paramref name="eventId"/>, invoke <paramref name="handlerInvocation"/> only on the
/// first delivery, persist the inbox marker, and commit. Implementations are owned by
/// concrete persistence packages (EF, Dapper) — never depend on a specific implementation
/// from outside those packages.
/// </summary>
public interface IInboxExecutor
{
    Task ExecuteAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handlerInvocation,
        CancellationToken ct = default);
}
```

- [ ] **Step 5: Build to verify**

Run: `dotnet build src/Shared/Shared.Abstractions.Messaging/Shared.Abstractions.Messaging.csproj`
Expected: success.

- [ ] **Step 6: Commit**

```bash
git add src/Shared/Shared.Abstractions.Messaging/
git commit -m "feat(shared): add integration-event bus, handler, inbox executor abstractions (#152)"
```

---

## Task 4: Core `Shared.Infrastructure.Messaging` — bus + worker + in-process transport

> ⚡ **Parallelizable** with Tasks 5 and 6 once Task 3 is in.

**Files (all new):**
- `src/Shared/Shared.Infrastructure.Messaging/Outbox/OutboxMessage.cs`
- `src/Shared/Shared.Infrastructure.Messaging/Outbox/IOutboxStore.cs`
- `src/Shared/Shared.Infrastructure.Messaging/Outbox/OutboxIntegrationEventBus.cs`
- `src/Shared/Shared.Infrastructure.Messaging/Outbox/OutboxWorker.cs`
- `src/Shared/Shared.Infrastructure.Messaging/Outbox/OutboxWorkerOptions.cs`
- `src/Shared/Shared.Infrastructure.Messaging/Transport/IIntegrationEventTransport.cs`
- `src/Shared/Shared.Infrastructure.Messaging/Transport/InProcessIntegrationEventTransport.cs`
- `src/Shared/Shared.Infrastructure.Messaging/Serialization/IntegrationEventSerializer.cs`
- `src/Shared/Shared.Infrastructure.Messaging/Extensions/MessagingBuilder.cs`
- `src/Shared/Shared.Infrastructure.Messaging/Extensions/MessagingExtensions.cs`
- Tests in `src/Shared/Shared.Messaging.Tests/`

### Step 1: `OutboxMessage.cs` (POCO)

A persistence-agnostic record describing an unpublished event. EF and Dapper convert to/from this in their stores.

```csharp
namespace Shared.Infrastructure.Messaging.Outbox;

public sealed record OutboxMessage(
    Guid Id,
    Guid EventId,
    string EventType,
    string Payload,
    DateTime OccurredAt,
    DateTime? ProcessedAt,
    int AttemptCount,
    string? LastError);
```

### Step 2: `IOutboxStore.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Outbox;

public interface IOutboxStore
{
    Task AddAsync(OutboxMessage message, CancellationToken ct);

    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken ct);

    Task MarkProcessedAsync(Guid messageId, DateTime processedAt, CancellationToken ct);

    Task RecordFailureAsync(Guid messageId, string error, CancellationToken ct);
}
```

### Step 3: `IntegrationEventSerializer.cs`

Encapsulates `JsonSerializer` so tests can substitute. Stateless, can be a singleton.

```csharp
namespace Shared.Infrastructure.Messaging.Serialization;

using System.Text.Json;
using Shared.Abstractions.Messaging;

public interface IIntegrationEventSerializer
{
    string Serialize(IIntegrationEvent @event);
    IIntegrationEvent Deserialize(string payload, string eventType);
}

public sealed class IntegrationEventSerializer : IIntegrationEventSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        PropertyNameCaseInsensitive = true
    };

    public string Serialize(IIntegrationEvent @event)
        => JsonSerializer.Serialize(@event, @event.GetType(), Options);

    public IIntegrationEvent Deserialize(string payload, string eventType)
    {
        var type = Type.GetType(eventType, throwOnError: true)
            ?? throw new InvalidOperationException($"Unknown event type: {eventType}");

        var result = JsonSerializer.Deserialize(payload, type, Options)
            ?? throw new InvalidOperationException($"Failed to deserialize event of type {eventType}");

        return (IIntegrationEvent)result;
    }
}
```

### Step 4: `OutboxIntegrationEventBus.cs` — TDD

- [ ] **4a — Write failing test:** `src/Shared/Shared.Messaging.Tests/Outbox/OutboxIntegrationEventBusTests.cs`

```csharp
namespace Shared.Messaging.Tests.Outbox;

using NSubstitute;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Serialization;
using Shouldly;

public sealed class OutboxIntegrationEventBusTests
{
    private readonly IOutboxStore _store = Substitute.For<IOutboxStore>();
    private readonly IIntegrationEventSerializer _serializer = Substitute.For<IIntegrationEventSerializer>();
    private readonly OutboxIntegrationEventBus _sut;

    public OutboxIntegrationEventBusTests()
        => _sut = new OutboxIntegrationEventBus(_store, _serializer);

    [Fact]
    public async Task PublishAsync_WithEvent_AddsSerializedOutboxMessage()
    {
        var eventId = Guid.NewGuid();
        var occurredAt = new DateTime(2026, 4, 25, 12, 0, 0, DateTimeKind.Utc);
        var @event = new TestIntegrationEvent(eventId, occurredAt, "hello");

        _serializer.Serialize(@event).Returns("""{"EventId":"...","Payload":"hello"}""");

        await _sut.PublishAsync(@event, CancellationToken.None);

        await _store.Received(1).AddAsync(
            Arg.Is<OutboxMessage>(m =>
                m.EventId == eventId &&
                m.OccurredAt == occurredAt &&
                m.EventType == typeof(TestIntegrationEvent).AssemblyQualifiedName &&
                m.Payload == """{"EventId":"...","Payload":"hello"}""" &&
                m.ProcessedAt == null &&
                m.AttemptCount == 0 &&
                m.LastError == null),
            Arg.Any<CancellationToken>());
    }

    private sealed record TestIntegrationEvent(Guid EventId, DateTime OccurredAt, string Payload)
        : IIntegrationEvent;
}
```

- [ ] **4b — Run, expect FAIL:** `dotnet test src/Shared/Shared.Messaging.Tests/ --filter PublishAsync_WithEvent_AddsSerializedOutboxMessage`
  Expected: compile error (`OutboxIntegrationEventBus` does not exist).

- [ ] **4c — Implement:** `src/Shared/Shared.Infrastructure.Messaging/Outbox/OutboxIntegrationEventBus.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Outbox;

using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Serialization;

public sealed class OutboxIntegrationEventBus : IIntegrationEventBus
{
    private readonly IOutboxStore _store;
    private readonly IIntegrationEventSerializer _serializer;

    public OutboxIntegrationEventBus(IOutboxStore store, IIntegrationEventSerializer serializer)
        => (_store, _serializer) = (store, serializer);

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var message = new OutboxMessage(
            Id: Guid.NewGuid(),
            EventId: @event.EventId,
            EventType: typeof(TEvent).AssemblyQualifiedName!,
            Payload: _serializer.Serialize(@event),
            OccurredAt: @event.OccurredAt,
            ProcessedAt: null,
            AttemptCount: 0,
            LastError: null);

        return _store.AddAsync(message, ct);
    }
}
```

> **Note:** `typeof(TEvent)` resolves to the static generic type. If a caller stores the
> event in a base reference, the test (which uses the concrete subtype) will still pass
> because `PublishAsync` is generic. To allow callers to publish via a base reference,
> add `@event.GetType().AssemblyQualifiedName` instead. Stick with `typeof(TEvent)` for
> v1 — it matches the spec's caller pattern.

- [ ] **4d — Run, expect PASS.**

### Step 5: `IIntegrationEventTransport.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Transport;

using Shared.Infrastructure.Messaging.Outbox;

public interface IIntegrationEventTransport
{
    Task DispatchAsync(OutboxMessage message, CancellationToken ct);
}
```

### Step 6: `InProcessIntegrationEventTransport.cs` — TDD

- [ ] **6a — Failing test:** `src/Shared/Shared.Messaging.Tests/Transport/InProcessIntegrationEventTransportTests.cs`

```csharp
namespace Shared.Messaging.Tests.Transport;

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Serialization;
using Shared.Infrastructure.Messaging.Transport;
using Shouldly;

public sealed class InProcessIntegrationEventTransportTests
{
    private static OutboxMessage MakeMessage<TEvent>(TEvent @event, IIntegrationEventSerializer serializer)
        where TEvent : IIntegrationEvent
        => new(
            Id: Guid.NewGuid(),
            EventId: @event.EventId,
            EventType: typeof(TEvent).AssemblyQualifiedName!,
            Payload: serializer.Serialize(@event),
            OccurredAt: @event.OccurredAt,
            ProcessedAt: null,
            AttemptCount: 0,
            LastError: null);

    [Fact]
    public async Task DispatchAsync_WithRegisteredHandler_ResolvesViaInboxExecutorAndInvokesHandler()
    {
        // Arrange
        var serializer = new IntegrationEventSerializer();
        var capturedEvent = (TestEvent?)null;

        var handler = Substitute.For<IIntegrationEventHandler<TestEvent>>();
        await handler.HandleAsync(Arg.Do<TestEvent>(e => capturedEvent = e), Arg.Any<CancellationToken>());

        var inbox = Substitute.For<IInboxExecutor>();
        inbox
            .ExecuteAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var invocation = ci.Arg<Func<CancellationToken, Task>>();
                await invocation(CancellationToken.None);
            });

        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventSerializer>(serializer);
        services.AddScoped<IIntegrationEventHandler<TestEvent>>(_ => handler);
        services.AddScoped<IInboxExecutor>(_ => inbox);
        var sp = services.BuildServiceProvider();

        var sut = new InProcessIntegrationEventTransport(sp);
        var @event = new TestEvent(Guid.NewGuid(), DateTime.UtcNow, "x");
        var message = MakeMessage(@event, serializer);

        // Act
        await sut.DispatchAsync(message, CancellationToken.None);

        // Assert — handler invoked with deserialized event
        capturedEvent.ShouldNotBeNull();
        capturedEvent!.Payload.ShouldBe("x");

        // Inbox executor was called with the right event id
        await inbox.Received(1).ExecuteAsync(
            @event.EventId, message.EventType, Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchAsync_WithNoHandlerRegistered_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventSerializer, IntegrationEventSerializer>();
        services.AddScoped<IInboxExecutor>(_ => Substitute.For<IInboxExecutor>());
        var sp = services.BuildServiceProvider();
        var sut = new InProcessIntegrationEventTransport(sp);

        var @event = new TestEvent(Guid.NewGuid(), DateTime.UtcNow, "x");
        var message = MakeMessage(@event, sp.GetRequiredService<IIntegrationEventSerializer>());

        var act = async () => await sut.DispatchAsync(message, CancellationToken.None);

        await act.ShouldNotThrowAsync();
    }

    public sealed record TestEvent(Guid EventId, DateTime OccurredAt, string Payload) : IIntegrationEvent;
}
```

- [ ] **6b — Run, expect FAIL.**

- [ ] **6c — Implement:** `src/Shared/Shared.Infrastructure.Messaging/Transport/InProcessIntegrationEventTransport.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Transport;

using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Serialization;

public sealed class InProcessIntegrationEventTransport : IIntegrationEventTransport
{
    private readonly IServiceProvider _rootProvider;

    public InProcessIntegrationEventTransport(IServiceProvider rootProvider)
        => _rootProvider = rootProvider;

    public async Task DispatchAsync(OutboxMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        var eventType = Type.GetType(message.EventType)
            ?? throw new InvalidOperationException($"Unknown event type: {message.EventType}");

        await using var scope = _rootProvider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var serializer = sp.GetRequiredService<IIntegrationEventSerializer>();
        var @event = serializer.Deserialize(message.Payload, message.EventType);

        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = sp.GetServices(handlerType).ToList();

        if (handlers.Count == 0) return;

        var inbox = sp.GetRequiredService<IInboxExecutor>();

        foreach (var handler in handlers)
        {
            await inbox.ExecuteAsync(
                eventId: message.EventId,
                eventType: message.EventType,
                handlerInvocation: async invocationCt =>
                {
                    var task = (Task)handlerType
                        .GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!
                        .Invoke(handler, [@event, invocationCt])!;
                    await task.ConfigureAwait(false);
                },
                ct: ct);
        }
    }
}
```

- [ ] **6d — Run, expect both tests PASS.**

### Step 7: `OutboxWorkerOptions.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Outbox;

public sealed class OutboxWorkerOptions
{
    public const string SectionName = "Messaging:Outbox";

    public int BatchSize { get; init; } = 50;
    public int PollIntervalMs { get; init; } = 1000;
}
```

### Step 8: `OutboxWorker.cs` — TDD

- [ ] **8a — Failing test:** `src/Shared/Shared.Messaging.Tests/Outbox/OutboxWorkerTests.cs`

The worker is a `BackgroundService`; tests drive `ExecuteAsync` directly through a thin protected-helper accessor *or* trigger one tick via a public `RunOnceAsync`. The cleanest path is a public `RunOnceAsync` method that the `BackgroundService` calls in its loop — testable without timing.

```csharp
namespace Shared.Messaging.Tests.Outbox;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Transport;
using Shouldly;

public sealed class OutboxWorkerTests
{
    private static OutboxMessage MakePending(Guid id) =>
        new(id, Guid.NewGuid(), "Some.Event, Some.Asm", "{}", DateTime.UtcNow, null, 0, null);

    private static IServiceScopeFactory ScopeFactoryWith(IOutboxStore store, IIntegrationEventTransport transport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(store);
        services.AddSingleton(transport);
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task RunOnceAsync_WithPendingMessages_DispatchesAndMarksProcessed()
    {
        var msg = MakePending(Guid.NewGuid());
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns([msg]);

        var transport = Substitute.For<IIntegrationEventTransport>();

        var sut = new OutboxWorker(
            ScopeFactoryWith(store, transport),
            Options.Create(new OutboxWorkerOptions()),
            NullLogger<OutboxWorker>.Instance);

        await sut.RunOnceAsync(CancellationToken.None);

        await transport.Received(1).DispatchAsync(msg, Arg.Any<CancellationToken>());
        await store.Received(1).MarkProcessedAsync(msg.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_WhenTransportThrows_RecordsFailureAndContinues()
    {
        var msg1 = MakePending(Guid.NewGuid());
        var msg2 = MakePending(Guid.NewGuid());

        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns([msg1, msg2]);

        var transport = Substitute.For<IIntegrationEventTransport>();
        transport
            .When(t => t.DispatchAsync(msg1, Arg.Any<CancellationToken>()))
            .Throw(new InvalidOperationException("boom"));

        var sut = new OutboxWorker(
            ScopeFactoryWith(store, transport),
            Options.Create(new OutboxWorkerOptions()),
            NullLogger<OutboxWorker>.Instance);

        await sut.RunOnceAsync(CancellationToken.None);

        await store.Received(1).RecordFailureAsync(msg1.Id, "boom", Arg.Any<CancellationToken>());
        await store.DidNotReceive().MarkProcessedAsync(msg1.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await transport.Received(1).DispatchAsync(msg2, Arg.Any<CancellationToken>());
        await store.Received(1).MarkProcessedAsync(msg2.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunOnceAsync_WithNoPendingMessages_DoesNothing()
    {
        var store = Substitute.For<IOutboxStore>();
        store.GetUnprocessedAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns([]);

        var transport = Substitute.For<IIntegrationEventTransport>();

        var sut = new OutboxWorker(
            ScopeFactoryWith(store, transport),
            Options.Create(new OutboxWorkerOptions()),
            NullLogger<OutboxWorker>.Instance);

        await sut.RunOnceAsync(CancellationToken.None);

        await transport.DidNotReceive().DispatchAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
    }
}
```

- [ ] **8b — Run, expect FAIL.**

- [ ] **8c — Implement:** `src/Shared/Shared.Infrastructure.Messaging/Outbox/OutboxWorker.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Outbox;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Messaging.Transport;

public sealed class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxWorkerOptions _options;
    private readonly ILogger<OutboxWorker> _logger;

    public OutboxWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxWorkerOptions> options,
        ILogger<OutboxWorker> logger)
        => (_scopeFactory, _options, _logger) = (scopeFactory, options.Value, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Outbox worker tick failed");
            }

            try
            {
                await Task.Delay(_options.PollIntervalMs, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
        var transport = scope.ServiceProvider.GetRequiredService<IIntegrationEventTransport>();

        var pending = await store.GetUnprocessedAsync(_options.BatchSize, ct).ConfigureAwait(false);
        if (pending.Count == 0) return;

        foreach (var message in pending)
        {
            try
            {
                await transport.DispatchAsync(message, ct).ConfigureAwait(false);
                await store.MarkProcessedAsync(message.Id, DateTime.UtcNow, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex,
                    "Failed to dispatch outbox message {MessageId} ({EventType})",
                    message.Id, message.EventType);
                await store.RecordFailureAsync(message.Id, ex.Message, ct).ConfigureAwait(false);
            }
        }
    }
}
```

- [ ] **8d — Run, expect all three tests PASS.**

### Step 9: `MessagingBuilder` + `MessagingExtensions`

```csharp
// src/Shared/Shared.Infrastructure.Messaging/Extensions/MessagingBuilder.cs
namespace Shared.Infrastructure.Messaging.Extensions;

using Microsoft.Extensions.DependencyInjection;

public sealed class MessagingBuilder
{
    public IServiceCollection Services { get; }

    public MessagingBuilder(IServiceCollection services) => Services = services;
}
```

```csharp
// src/Shared/Shared.Infrastructure.Messaging/Extensions/MessagingExtensions.cs
namespace Shared.Infrastructure.Messaging.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Serialization;
using Shared.Infrastructure.Messaging.Transport;

public static class MessagingExtensions
{
    public static MessagingBuilder AddIntegrationEventBus(this IServiceCollection services)
    {
        services.TryAddSingleton<IIntegrationEventSerializer, IntegrationEventSerializer>();
        services.TryAddScoped<IIntegrationEventBus, OutboxIntegrationEventBus>();
        services.AddOptions<OutboxWorkerOptions>();
        services.AddHostedService<OutboxWorker>();

        return new MessagingBuilder(services);
    }

    public static MessagingBuilder UseInProcessTransport(this MessagingBuilder builder)
    {
        builder.Services.TryAddSingleton<IIntegrationEventTransport, InProcessIntegrationEventTransport>();
        return builder;
    }
}
```

- [ ] **Step 10: Build full solution**

Run: `dotnet build HomeSystem.slnx`
Expected: success.

- [ ] **Step 11: Run all messaging tests**

Run: `dotnet test src/Shared/Shared.Messaging.Tests/Shared.Messaging.Tests.csproj`
Expected: 6 tests pass.

- [ ] **Step 12: Commit**

```bash
git add src/Shared/Shared.Infrastructure.Messaging src/Shared/Shared.Messaging.Tests
git commit -m "feat(shared): add integration-event bus, outbox worker, in-process transport (#152)"
```

---

## Task 5: EF outbox + inbox in `Shared.Infrastructure.Messaging.Ef`

> ⚡ **Parallelizable** with Tasks 4 and 6.

**Files (all new):**
- `src/Shared/Shared.Infrastructure.Messaging.Ef/Outbox/OutboxMessageEntity.cs`
- `src/Shared/Shared.Infrastructure.Messaging.Ef/Outbox/OutboxMessageEntityConfiguration.cs`
- `src/Shared/Shared.Infrastructure.Messaging.Ef/Outbox/EfOutboxStore.cs`
- `src/Shared/Shared.Infrastructure.Messaging.Ef/Inbox/InboxMessageEntity.cs`
- `src/Shared/Shared.Infrastructure.Messaging.Ef/Inbox/InboxMessageEntityConfiguration.cs`
- `src/Shared/Shared.Infrastructure.Messaging.Ef/Inbox/EfInboxExecutor.cs`
- `src/Shared/Shared.Infrastructure.Messaging.Ef/Extensions/EfMessagingExtensions.cs`

> Tests for these types are integration-only (require Postgres) and live in Task 7.
> Unit-testing the EF behaviour is low value — the contract is "EF persists what you ask
> it to persist" — and easy to fake into uselessness with InMemory provider. Roundtrip
> tests against real Postgres are the meaningful coverage.

### Step 1: `OutboxMessageEntity.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Ef.Outbox;

internal sealed class OutboxMessageEntity
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public string EventType { get; init; } = default!;
    public string Payload { get; init; } = default!;
    public DateTime OccurredAt { get; init; }
    public DateTime? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}
```

### Step 2: `OutboxMessageEntityConfiguration.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Ef.Outbox;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EventId).HasColumnName("event_id");
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        builder.Property(x => x.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0);
        builder.Property(x => x.LastError).HasColumnName("last_error");

        builder.HasIndex(x => x.ProcessedAt)
               .HasFilter(""""processed_at"" IS NULL"")
               .HasDatabaseName("ix_outbox_messages_unprocessed");
    }
}
```

> The HasFilter literal uses Postgres double-quoted identifiers; it is exactly the SQL that
> appears in the migration. If the consuming module uses a global snake_case convention,
> the filter should be reviewed — see `backend-ef-core-patterns.md`.

### Step 3: `EfOutboxStore.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Ef.Outbox;

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Outbox;

internal sealed class EfOutboxStore<TDbContext> : IOutboxStore
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    public EfOutboxStore(TDbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(OutboxMessage message, CancellationToken ct)
        => _dbContext.Set<OutboxMessageEntity>()
            .AddAsync(MapToEntity(message), ct).AsTask();

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken ct)
        => await _dbContext.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.OccurredAt)
            .Take(batchSize)
            .Select(x => new OutboxMessage(
                x.Id, x.EventId, x.EventType, x.Payload, x.OccurredAt,
                x.ProcessedAt, x.AttemptCount, x.LastError))
            .ToListAsync(ct);

    public async Task MarkProcessedAsync(Guid messageId, DateTime processedAt, CancellationToken ct)
    {
        await _dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProcessedAt, processedAt), ct);
    }

    public async Task RecordFailureAsync(Guid messageId, string error, CancellationToken ct)
    {
        await _dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.LastError, error)
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), ct);
    }

    private static OutboxMessageEntity MapToEntity(OutboxMessage m) => new()
    {
        Id = m.Id,
        EventId = m.EventId,
        EventType = m.EventType,
        Payload = m.Payload,
        OccurredAt = m.OccurredAt,
        ProcessedAt = m.ProcessedAt,
        AttemptCount = m.AttemptCount,
        LastError = m.LastError
    };
}
```

### Step 4: `InboxMessageEntity.cs` + `InboxMessageEntityConfiguration.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Ef.Inbox;

internal sealed class InboxMessageEntity
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = default!;
    public DateTime ConsumedAt { get; init; }
}
```

```csharp
namespace Shared.Infrastructure.Messaging.Ef.Inbox;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class InboxMessageEntityConfiguration : IEntityTypeConfiguration<InboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<InboxMessageEntity> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(x => x.EventId);

        builder.Property(x => x.EventId).HasColumnName("event_id");
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ConsumedAt).HasColumnName("consumed_at");
    }
}
```

### Step 5: `EfInboxExecutor.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Ef.Inbox;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Messaging;

internal sealed class EfInboxExecutor<TDbContext> : IInboxExecutor
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    public EfInboxExecutor(TDbContext dbContext) => _dbContext = dbContext;

    public async Task ExecuteAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handlerInvocation,
        CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var alreadyConsumed = await _dbContext.Set<InboxMessageEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.EventId == eventId, ct)
            .ConfigureAwait(false);

        if (alreadyConsumed)
        {
            await transaction.RollbackAsync(ct).ConfigureAwait(false);
            return;
        }

        await handlerInvocation(ct).ConfigureAwait(false);

        await _dbContext.Set<InboxMessageEntity>()
            .AddAsync(new InboxMessageEntity
            {
                EventId = eventId,
                EventType = eventType,
                ConsumedAt = DateTime.UtcNow
            }, ct)
            .ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }
}
```

### Step 6: `EfMessagingExtensions.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Ef.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Ef.Inbox;
using Shared.Infrastructure.Messaging.Ef.Outbox;
using Shared.Infrastructure.Messaging.Outbox;

public static class EfMessagingExtensions
{
    public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IOutboxStore, EfOutboxStore<TDbContext>>();
        return services;
    }

    public static IServiceCollection AddInbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IInboxExecutor, EfInboxExecutor<TDbContext>>();
        return services;
    }
}
```

- [ ] **Step 7: Build**

Run: `dotnet build HomeSystem.slnx`
Expected: success.

- [ ] **Step 8: Commit**

```bash
git add src/Shared/Shared.Infrastructure.Messaging.Ef
git commit -m "feat(shared): add EF outbox + inbox executor (#152)"
```

---

## Task 6: Dapper inbox in `Shared.Infrastructure.Messaging.Dapper`

> ⚡ **Parallelizable** with Tasks 4 and 5.

**Files (all new):**
- `src/Shared/Shared.Infrastructure.Messaging.Dapper/INpgsqlConnectionFactory.cs`
- `src/Shared/Shared.Infrastructure.Messaging.Dapper/Inbox/InboxSql.cs`
- `src/Shared/Shared.Infrastructure.Messaging.Dapper/Inbox/DapperInboxExecutor.cs`
- `src/Shared/Shared.Infrastructure.Messaging.Dapper/Extensions/DapperMessagingExtensions.cs`

> No Dapper outbox in v1. The only publisher (DietPlanner, N6) is EF-backed. When the
> first Dapper publisher arrives, add `DapperOutboxStore` symmetric to `EfOutboxStore`.

### Step 1: `INpgsqlConnectionFactory.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Dapper;

using Npgsql;

public interface INpgsqlConnectionFactory
{
    Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default);
}
```

> Module-side: `NotificationsConnectionFactory` (defined in N3) implements this so the
> shared `DapperInboxExecutor` is parameterised on the module's pool.

### Step 2: `InboxSql.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Dapper.Inbox;

internal static class InboxSql
{
    internal const string Exists = """
        SELECT EXISTS (SELECT 1 FROM inbox_messages WHERE event_id = @EventId);
        """;

    internal const string Insert = """
        INSERT INTO inbox_messages (event_id, event_type, consumed_at)
        VALUES (@EventId, @EventType, @ConsumedAt);
        """;
}
```

### Step 3: `DapperInboxExecutor.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Dapper.Inbox;

using global::Dapper;
using Shared.Abstractions.Messaging;

internal sealed class DapperInboxExecutor<TFactory> : IInboxExecutor
    where TFactory : INpgsqlConnectionFactory
{
    private readonly TFactory _factory;

    public DapperInboxExecutor(TFactory factory) => _factory = factory;

    public async Task ExecuteAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handlerInvocation,
        CancellationToken ct = default)
    {
        await using var connection = await _factory.OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

        var exists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(InboxSql.Exists, new { EventId = eventId },
                transaction: transaction, cancellationToken: ct))
            .ConfigureAwait(false);

        if (exists)
        {
            await transaction.RollbackAsync(ct).ConfigureAwait(false);
            return;
        }

        await handlerInvocation(ct).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(InboxSql.Insert,
                new { EventId = eventId, EventType = eventType, ConsumedAt = DateTime.UtcNow },
                transaction: transaction, cancellationToken: ct))
            .ConfigureAwait(false);

        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }
}
```

> Note: the handler in this executor is invoked **inside** the Dapper transaction, but
> the handler does its own work on a different connection (its own scoped `DapperUnitOfWork`).
> That's fine for a Dapper-backed *consumer*: the inbox row + the handler's work commit on
> the consumer's pool, but they are not in the same DB transaction. If a future module
> needs strict transactional consistency, it must implement a custom `IInboxExecutor` that
> shares the connection with the handler. For v1, idempotency from the inbox check is
> sufficient — the handler is expected to be itself transactional.

### Step 4: `DapperMessagingExtensions.cs`

```csharp
namespace Shared.Infrastructure.Messaging.Dapper.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Dapper.Inbox;

public static class DapperMessagingExtensions
{
    public static IServiceCollection AddDapperInbox<TFactory>(this IServiceCollection services)
        where TFactory : class, INpgsqlConnectionFactory
    {
        services.AddScoped<IInboxExecutor, DapperInboxExecutor<TFactory>>();
        return services;
    }
}
```

- [ ] **Step 5: Build**

Run: `dotnet build HomeSystem.slnx`
Expected: success.

- [ ] **Step 6: Commit**

```bash
git add src/Shared/Shared.Infrastructure.Messaging.Dapper
git commit -m "feat(shared): add Dapper inbox executor (#152)"
```

---

## Task 7: Integration tests with Testcontainers (Postgres)

**Files (all new):**
- `src/Shared/Shared.Messaging.IntegrationTests/Fixtures/PostgresContainerFixture.cs`
- `src/Shared/Shared.Messaging.IntegrationTests/Fixtures/MessagingTestDbContext.cs`
- `src/Shared/Shared.Messaging.IntegrationTests/Ef/EfInboxExecutorIntegrationTests.cs`
- `src/Shared/Shared.Messaging.IntegrationTests/Ef/EfOutboxStoreIntegrationTests.cs`
- `src/Shared/Shared.Messaging.IntegrationTests/Dapper/TestNpgsqlConnectionFactory.cs`
- `src/Shared/Shared.Messaging.IntegrationTests/Dapper/DapperInboxExecutorIntegrationTests.cs`
- `src/Shared/Shared.Messaging.IntegrationTests/Roundtrip/InProcessRoundtripIntegrationTests.cs`

### Step 1: `PostgresContainerFixture.cs` (xUnit collection fixture)

```csharp
namespace Shared.Messaging.IntegrationTests.Fixtures;

using Testcontainers.PostgreSql;
using Xunit;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("messaging_tests")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync() => await Container.StartAsync();

    public async Task DisposeAsync() => await Container.DisposeAsync();
}

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture> { }
```

### Step 2: `MessagingTestDbContext.cs`

This is the test-only DbContext that registers the Outbox + Inbox configurations and runs
EnsureCreated (no migrations needed for tests).

```csharp
namespace Shared.Messaging.IntegrationTests.Fixtures;

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Ef.Inbox;
using Shared.Infrastructure.Messaging.Ef.Outbox;

internal sealed class MessagingTestDbContext : DbContext
{
    public MessagingTestDbContext(DbContextOptions<MessagingTestDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
```

### Step 3: `EfOutboxStoreIntegrationTests.cs`

```csharp
namespace Shared.Messaging.IntegrationTests.Ef;

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Ef.Outbox;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Messaging.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

[Collection(nameof(PostgresCollection))]
public sealed class EfOutboxStoreIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private MessagingTestDbContext _dbContext = default!;

    public EfOutboxStoreIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<MessagingTestDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        _dbContext = new MessagingTestDbContext(options);
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _dbContext.DisposeAsync();

    [Fact]
    public async Task AddAsync_PersistsRow()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var msg = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", """{"a":1}""",
            DateTime.UtcNow, null, 0, null);

        await sut.AddAsync(msg, default);
        await _dbContext.SaveChangesAsync();

        var unprocessed = await sut.GetUnprocessedAsync(10, default);
        unprocessed.Count.ShouldBe(1);
        unprocessed[0].EventId.ShouldBe(msg.EventId);
    }

    [Fact]
    public async Task MarkProcessedAsync_RemovesFromUnprocessed()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var msg = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", "{}",
            DateTime.UtcNow, null, 0, null);

        await sut.AddAsync(msg, default);
        await _dbContext.SaveChangesAsync();
        await sut.MarkProcessedAsync(msg.Id, DateTime.UtcNow, default);

        var unprocessed = await sut.GetUnprocessedAsync(10, default);
        unprocessed.ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordFailureAsync_IncrementsAttemptCountAndStoresError()
    {
        var sut = new EfOutboxStore<MessagingTestDbContext>(_dbContext);
        var msg = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), "X.Y", "{}",
            DateTime.UtcNow, null, 0, null);

        await sut.AddAsync(msg, default);
        await _dbContext.SaveChangesAsync();

        await sut.RecordFailureAsync(msg.Id, "boom", default);
        await sut.RecordFailureAsync(msg.Id, "boom2", default);

        var unprocessed = await sut.GetUnprocessedAsync(10, default);
        unprocessed[0].AttemptCount.ShouldBe(2);
        unprocessed[0].LastError.ShouldBe("boom2");
    }
}
```

### Step 4: `EfInboxExecutorIntegrationTests.cs`

```csharp
namespace Shared.Messaging.IntegrationTests.Ef;

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Ef.Inbox;
using Shared.Messaging.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

[Collection(nameof(PostgresCollection))]
public sealed class EfInboxExecutorIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private MessagingTestDbContext _dbContext = default!;

    public EfInboxExecutorIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<MessagingTestDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        _dbContext = new MessagingTestDbContext(options);
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _dbContext.DisposeAsync();

    [Fact]
    public async Task ExecuteAsync_FirstCall_InvokesHandlerAndPersistsInboxRow()
    {
        var sut = new EfInboxExecutor<MessagingTestDbContext>(_dbContext);
        var eventId = Guid.NewGuid();
        var invocations = 0;

        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);

        invocations.ShouldBe(1);
        var rowExists = await _dbContext.Set<InboxMessageEntity>().AnyAsync(x => x.EventId == eventId);
        rowExists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_SecondCallSameEventId_SkipsHandler()
    {
        var sut = new EfInboxExecutor<MessagingTestDbContext>(_dbContext);
        var eventId = Guid.NewGuid();
        var invocations = 0;

        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);
        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);

        invocations.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_HandlerThrows_DoesNotPersistInboxRow()
    {
        var sut = new EfInboxExecutor<MessagingTestDbContext>(_dbContext);
        var eventId = Guid.NewGuid();

        var act = () => sut.ExecuteAsync(
            eventId, "X.Y",
            _ => throw new InvalidOperationException("boom"),
            default);

        await act.ShouldThrowAsync<InvalidOperationException>();

        var rowExists = await _dbContext.Set<InboxMessageEntity>().AnyAsync(x => x.EventId == eventId);
        rowExists.ShouldBeFalse();
    }
}
```

### Step 5: `TestNpgsqlConnectionFactory.cs`

```csharp
namespace Shared.Messaging.IntegrationTests.Dapper;

using Npgsql;
using Shared.Infrastructure.Messaging.Dapper;

internal sealed class TestNpgsqlConnectionFactory : INpgsqlConnectionFactory
{
    private readonly string _connectionString;

    public TestNpgsqlConnectionFactory(string connectionString) => _connectionString = connectionString;

    public async Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
```

### Step 6: `DapperInboxExecutorIntegrationTests.cs`

```csharp
namespace Shared.Messaging.IntegrationTests.Dapper;

using global::Dapper;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Dapper.Inbox;
using Shared.Messaging.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

[Collection(nameof(PostgresCollection))]
public sealed class DapperInboxExecutorIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private MessagingTestDbContext _dbContext = default!;

    public DapperInboxExecutorIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<MessagingTestDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        _dbContext = new MessagingTestDbContext(options);
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _dbContext.DisposeAsync();

    [Fact]
    public async Task ExecuteAsync_FirstCall_InvokesHandlerAndInsertsRow()
    {
        var factory = new TestNpgsqlConnectionFactory(_fixture.ConnectionString);
        var sut = new DapperInboxExecutor<TestNpgsqlConnectionFactory>(factory);
        var eventId = Guid.NewGuid();
        var invocations = 0;

        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);

        invocations.ShouldBe(1);
        await using var conn = await factory.OpenAsync();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM inbox_messages WHERE event_id = @EventId", new { EventId = eventId });
        count.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_SecondCallSameEventId_SkipsHandler()
    {
        var factory = new TestNpgsqlConnectionFactory(_fixture.ConnectionString);
        var sut = new DapperInboxExecutor<TestNpgsqlConnectionFactory>(factory);
        var eventId = Guid.NewGuid();
        var invocations = 0;

        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);
        await sut.ExecuteAsync(eventId, "X.Y", _ => { invocations++; return Task.CompletedTask; }, default);

        invocations.ShouldBe(1);
    }
}
```

### Step 7: `InProcessRoundtripIntegrationTests.cs`

```csharp
namespace Shared.Messaging.IntegrationTests.Roundtrip;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Ef.Extensions;
using Shared.Infrastructure.Messaging.Extensions;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Messaging.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

[Collection(nameof(PostgresCollection))]
public sealed class InProcessRoundtripIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private ServiceProvider _sp = default!;

    public InProcessRoundtripIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

    public sealed record HelloIntegrationEvent(Guid EventId, DateTime OccurredAt, string Greeting)
        : IIntegrationEvent;

    public sealed class HelloHandler : IIntegrationEventHandler<HelloIntegrationEvent>
    {
        public List<HelloIntegrationEvent> Received { get; } = new();
        public Task HandleAsync(HelloIntegrationEvent @event, CancellationToken ct = default)
        {
            Received.Add(@event);
            return Task.CompletedTask;
        }
    }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddDbContext<MessagingTestDbContext>(o => o.UseNpgsql(_fixture.ConnectionString));
        services.AddIntegrationEventBus().UseInProcessTransport();
        services.AddOutbox<MessagingTestDbContext>();
        services.AddInbox<MessagingTestDbContext>();

        services.AddSingleton<HelloHandler>();
        services.AddScoped<IIntegrationEventHandler<HelloIntegrationEvent>>(sp =>
            sp.GetRequiredService<HelloHandler>());

        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        _sp = services.BuildServiceProvider();

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MessagingTestDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _sp.DisposeAsync();

    [Fact]
    public async Task Publish_ThenRunOnce_HandlerReceivesEvent()
    {
        // Publish via bus
        await using (var scope = _sp.CreateAsyncScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();
            var db = scope.ServiceProvider.GetRequiredService<MessagingTestDbContext>();

            await bus.PublishAsync(
                new HelloIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "hi"),
                default);
            await db.SaveChangesAsync();
        }

        // Run worker tick
        var worker = new OutboxWorker(
            _sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxWorkerOptions()),
            NullLogger<OutboxWorker>.Instance);

        await worker.RunOnceAsync(default);

        // Assert
        var handler = _sp.GetRequiredService<HelloHandler>();
        handler.Received.Count.ShouldBe(1);
        handler.Received[0].Greeting.ShouldBe("hi");
    }

    [Fact]
    public async Task Publish_SameEventIdTwice_HandlerOnlyReceivesOnce()
    {
        var eventId = Guid.NewGuid();

        async Task PublishAsync()
        {
            await using var scope = _sp.CreateAsyncScope();
            var bus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();
            var db = scope.ServiceProvider.GetRequiredService<MessagingTestDbContext>();
            await bus.PublishAsync(new HelloIntegrationEvent(eventId, DateTime.UtcNow, "hi"), default);
            await db.SaveChangesAsync();
        }

        await PublishAsync();
        await PublishAsync();

        var worker = new OutboxWorker(
            _sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxWorkerOptions()),
            NullLogger<OutboxWorker>.Instance);
        await worker.RunOnceAsync(default);

        var handler = _sp.GetRequiredService<HelloHandler>();
        handler.Received.Count.ShouldBe(1);
    }
}
```

- [ ] **Step 8: Run integration tests**

Run: `dotnet test src/Shared/Shared.Messaging.IntegrationTests/Shared.Messaging.IntegrationTests.csproj`
Expected: all 11 tests pass. Requires Docker daemon running (Testcontainers).

- [ ] **Step 9: Commit**

```bash
git add src/Shared/Shared.Messaging.IntegrationTests
git commit -m "test(shared): integration tests for messaging EF/Dapper executors and roundtrip (#152)"
```

---

## Task 8: Wire skeletal host registration + final cleanup

**Files:**
- Modify: `src/Apis/HomeSystem.REST/HomeSystem.REST.csproj`
- Modify: `src/Apis/HomeSystem.REST/Program.cs`
- Modify: `CLAUDE.md`

The host needs to register the bus + transport so subsequent issues (N3, N6) only add their
own outbox/inbox bindings. No handlers are registered yet — there are no integration events
in scope until N6.

- [ ] **Step 1: Add project references to host**

```xml
<!-- HomeSystem.REST.csproj — add to existing ItemGroup of project references -->
<ProjectReference Include="..\..\Shared\Shared.Infrastructure.Messaging\Shared.Infrastructure.Messaging.csproj" />
```

> Do **not** reference `Shared.Infrastructure.Messaging.Ef` or `.Dapper` from the host —
> those are referenced from individual modules in N3 / N6. The host only owns the bus +
> transport, which are persistence-agnostic.

- [ ] **Step 2: Wire DI in `Program.cs`**

Find the existing `builder.Services` chain and append before the existing module
registrations:

```csharp
builder.Services
    .AddIntegrationEventBus()
    .UseInProcessTransport();
```

Add `using Shared.Infrastructure.Messaging.Extensions;` at the top.

> Note: The `OutboxWorker` is registered as a hosted service by `AddIntegrationEventBus`.
> With no `IOutboxStore` registered yet, the worker will throw on its first tick. Until
> a publisher arrives (N6), guard the registration:

Update `MessagingExtensions.AddIntegrationEventBus` to register the hosted service only
when a store is available. Simpler: leave the worker registered but make it bail
gracefully when no `IOutboxStore` is registered. Update `OutboxWorker.RunOnceAsync`:

```csharp
public async Task RunOnceAsync(CancellationToken ct)
{
    await using var scope = _scopeFactory.CreateAsyncScope();
    var store = scope.ServiceProvider.GetService<IOutboxStore>();
    if (store is null) return;                      // ← graceful no-op
    var transport = scope.ServiceProvider.GetService<IIntegrationEventTransport>();
    if (transport is null) return;
    // … rest unchanged
}
```

Update the existing tests in Task 4 step 8 to register both services (they already do).
Add one new test:

```csharp
[Fact]
public async Task RunOnceAsync_WithNoStoreRegistered_ReturnsWithoutThrowing()
{
    var services = new ServiceCollection();
    var sp = services.BuildServiceProvider();
    var sut = new OutboxWorker(sp.GetRequiredService<IServiceScopeFactory>(),
        Options.Create(new OutboxWorkerOptions()),
        NullLogger<OutboxWorker>.Instance);

    var act = () => sut.RunOnceAsync(default);
    await act.ShouldNotThrowAsync();
}
```

- [ ] **Step 3: Update CLAUDE.md repository structure**

Replace the `Shared/` block with the expanded list:

```
  Shared/
    Shared.Abstractions.Core/         # Domain primitives (IDomainEvent, AggregateRoot, Entity, IUnitOfWork, exceptions, PagedList)
    Shared.Abstractions.Cqrs/         # ICommand, IQuery, dispatchers, handlers, validators
    Shared.Abstractions.Messaging/    # IIntegrationEvent, IIntegrationEventBus, IIntegrationEventHandler, IInboxExecutor
    Shared.Infrastructure.Cqrs/       # CQRS dispatcher implementation + decorators
    Shared.Infrastructure.Messaging/         # Outbox bus, worker, in-process transport, JSON serializer
    Shared.Infrastructure.Messaging.Ef/      # EF outbox + inbox executor (parameterised on TDbContext)
    Shared.Infrastructure.Messaging.Dapper/  # Dapper inbox executor (parameterised on INpgsqlConnectionFactory)
    Shared.Infrastructure.Persistence/# EF Core interceptors (DomainEventDispatcherInterceptor)
    Shared.Infrastructure.Web/        # Cross-cutting web middleware (ExceptionHandlingMiddleware)
```

- [ ] **Step 4: Build + run unit + integration tests**

```bash
dotnet build HomeSystem.slnx
dotnet test src/Shared/Shared.Messaging.Tests/Shared.Messaging.Tests.csproj
dotnet test src/Shared/Shared.Messaging.IntegrationTests/Shared.Messaging.IntegrationTests.csproj
dotnet test src/Modules/DietPlanner/DietPlanner.UnitTests/DietPlanner.UnitTests.csproj
dotnet test src/Modules/DietPlanner/DietPlanner.IntegrationTests/DietPlanner.IntegrationTests.csproj
```

Expected: everything green; existing DietPlanner tests still pass (the messaging plumbing
should not affect any of them — DietPlanner doesn't reference messaging yet).

- [ ] **Step 5: Smoke-run the host**

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Apis/HomeSystem.REST
```

Expected: starts cleanly. Logs show `OutboxWorker` started. No exceptions on the first
tick (graceful no-op because no `IOutboxStore` is registered yet).

Stop with Ctrl-C.

- [ ] **Step 6: Commit**

```bash
git add src/Apis/HomeSystem.REST CLAUDE.md src/Shared/Shared.Infrastructure.Messaging/Outbox/OutboxWorker.cs src/Shared/Shared.Messaging.Tests
git commit -m "feat(host): wire integration-event bus + in-process transport (#152)"
```

- [ ] **Step 7: Push branch + open PR**

```bash
git push -u origin feat/152-integration-event-bus
gh pr create --title "feat(shared): integration-event bus + outbox/inbox + in-process transport" \
  --body "$(cat <<'EOF'
## Summary
- Adds `Shared.Infrastructure.Messaging` (bus, worker, in-process transport, JSON serializer).
- Adds `Shared.Infrastructure.Messaging.Ef` (outbox + inbox executor parameterised on `TDbContext`).
- Adds `Shared.Infrastructure.Messaging.Dapper` (inbox executor parameterised on `INpgsqlConnectionFactory`).
- Wires `AddIntegrationEventBus().UseInProcessTransport()` on the host.

Implements N2 from the notifications module design — closes #152.

## Test plan
- [ ] `dotnet test src/Shared/Shared.Messaging.Tests/` (unit) green
- [ ] `dotnet test src/Shared/Shared.Messaging.IntegrationTests/` (Testcontainers) green
- [ ] Existing `DietPlanner` tests still green
- [ ] Host boots; `OutboxWorker` is a graceful no-op until a publisher (N6) registers an outbox store
EOF
)"
```

---

## Self-review checklist (before declaring the plan complete)

- ✅ Spec §6.1 abstractions: `IIntegrationEvent` (Task 3 Step 1), `IIntegrationEventBus`
  (Task 3 Step 2), `IIntegrationEventHandler<T>` (Task 3 Step 3), `IInboxExecutor`
  (Task 3 Step 4).
- ✅ Spec §6.2 implementations: `OutboxIntegrationEventBus` (Task 4 Step 4),
  `OutboxWorker` (Task 4 Step 8), `IIntegrationEventTransport` +
  `InProcessIntegrationEventTransport` (Task 4 Step 6),
  `EfInboxExecutor<TDbContext>` (Task 5 Step 5),
  `DapperInboxExecutor` (Task 6 Step 3).
- ✅ Spec §6.3 outbox + inbox tables: outbox (Task 5 Step 2), inbox (Task 5 Step 4).
  Schema: `id`, `event_id`, `event_type`, `payload (jsonb)`, `occurred_at`,
  `processed_at`, `attempt_count`, `last_error` for outbox; `event_id`,
  `event_type`, `consumed_at` for inbox; partial index on `processed_at IS NULL`.
- ✅ Spec §6.4 module wiring: `AddIntegrationEventBus().UseInProcessTransport()`
  (Task 4 Step 9), `AddOutbox<TDbContext>()` and `AddInbox<TDbContext>()` (Task 5 Step 6),
  `AddDapperInbox<TFactory>()` (Task 6 Step 4).
- ✅ Spec §9.1 testing: bus writes outbox row (Task 4 Step 4), worker dispatches +
  marks processed + records failure (Task 4 Step 8), in-process transport resolves +
  dispatches (Task 4 Step 6), EF/Dapper executors are idempotent (Task 7 Steps 4 + 6),
  full roundtrip including duplicate skip (Task 7 Step 7).
- ✅ Type consistency: `OutboxMessage` record fields match across bus, store, worker,
  and transport. `IInboxExecutor` signature is identical in abstractions and both
  implementations. `INpgsqlConnectionFactory.OpenAsync` matches both the test factory
  and the consumer module's expected implementation (per `backend-dapper-module-structure.md`).
- ✅ No placeholders. Every step contains the exact code or command needed.

---

## Execution

**Plan complete and saved to** `docs/superpowers/plans/2026-04-25-shared-messaging.md`.

Two execution options:

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration. Tasks 4, 5, 6 dispatched in parallel.

**2. Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints.

Which approach?
