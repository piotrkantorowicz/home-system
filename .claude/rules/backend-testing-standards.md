# Backend — Testing Standards

> **Stack:** xUnit 2.9 (migration to xunit.v3 tracked in #269), Shouldly,
> NSubstitute, Testcontainers.PostgreSql, `FakeTimeProvider`.
> FluentAssertions is deliberately absent — do not add it.

## Test Project Layout

Tests are **co-located with the module**, one unit and one integration project each:

```
src/Modules/Household/
  Household.UnitTests/
    Domain/                     HouseholdTests.cs, HouseholdMemberTests.cs, …
    Application/                CreateHouseholdCommandHandlerTests.cs, …
      EventHandlers/
    Builders/                   HouseholdBuilder.cs
  Household.IntegrationTests/
    Api/                        HouseholdEndpointsTests.cs  (HTTP → handler → real DB)
    Persistence/                repository round-trips
    Fixtures/                   HouseholdWebApplicationFactory.cs
src/Shared/
  Shared.Messaging.Tests/       unit tests for the bus / outbox / decorators
  Shared.Messaging.IntegrationTests/
```

Every `*Tests.csproj` under `src/` is picked up by `dotnet test HomeSystem.slnx` in CI and by
`scripts/verify.sh` for the touched module.

## Unit Tests — Domain

Test domain logic through aggregate methods. No infrastructure, no mocks of your own domain.
Time is a parameter, so tests pass a fixed value.

```csharp
public sealed class HouseholdTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RemoveMember_WhenLastOwner_ThrowsDomainException()
    {
        var household = new HouseholdBuilder().WithOwner(PersonId.New()).Build();
        var owner = household.Members.Single();

        var act = () => household.RemoveMember(owner.PersonId, Now);

        act.ShouldThrow<HouseholdDomainException>().Message.ShouldContain("owner");
    }

    [Fact]
    public void Create_Always_RaisesHouseholdCreatedDomainEvent()
    {
        var household = Household.Create(HouseholdId.New(), "Home", PersonId.New(), Now);

        household.DomainEvents.ShouldContain(e => e is HouseholdCreatedDomainEvent);
    }
}
```

## Unit Tests — Application (handlers)

Mock only the boundaries: repositories, unit of work, `IIntegrationEventBus`, external services.
Inject `FakeTimeProvider` where the handler reads the clock.

```csharp
public sealed class CreateHouseholdCommandHandlerTests
{
    private readonly IHouseholdRepository _households = Substitute.For<IHouseholdRepository>();
    private readonly IHouseholdUnitOfWork _unitOfWork = Substitute.For<IHouseholdUnitOfWork>();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero));
    private readonly CreateHouseholdCommandHandler _sut;

    public CreateHouseholdCommandHandlerTests()
        => _sut = new CreateHouseholdCommandHandler(_households, _unitOfWork, _clock);

    [Fact]
    public async Task Handle_WithValidCommand_AddsHouseholdAndCommits()
    {
        var command = new CreateHouseholdCommand("sub-1", "Home");

        await _sut.HandleAsync(command, CancellationToken.None);

        await _households.Received(1).AddAsync(
            Arg.Is<Household>(h => h.Name == "Home" && h.CreatedAt == _clock.GetUtcNow()),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
```

### Unit test rules

- No database, filesystem, network, or real clock.
- `Substitute.For<T>()` for mocks; `FakeTimeProvider` for time; `_clock.Advance(...)` to move it.
- Shouldly only: `.ShouldBe`, `.ShouldThrow<T>`, `.ShouldNotBeNull`, `.ShouldHaveSingleItem`,
  `.ShouldBeOfType<T>`, `.ShouldContain(predicate)`. No `Assert.*`.
- One behaviour per test. `[Theory]` + `[InlineData]` for value variations only.
- Test names: `Method_State_Expected`. No `Async` suffix on test methods.
- Test classes are `sealed`; test project namespaces mirror the module (`Household.UnitTests.Domain`).

## Integration Tests — API

Full vertical slice: HTTP request → endpoint → dispatcher → handler → real PostgreSQL → response.

```csharp
public sealed class HouseholdEndpointsTests(HouseholdWebApplicationFactory factory)
    : IClassFixture<HouseholdWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClientAs("sub-owner");

    [Fact]
    public async Task POST_Households_WithValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/households", new CreateHouseholdRequest("Home"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
    }

    [Fact]
    public async Task GET_MyHousehold_WhenNone_Returns404()
    {
        var response = await factory.CreateClientAs("sub-nobody").GetAsync("/api/households/me");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
```

```csharp
public sealed class HouseholdWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().WithDatabase("household_test").Build();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero));

    public Task InitializeAsync() => _postgres.StartAsync();
    public new Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<HouseholdDbContext>>();
            services.AddDbContext<HouseholdDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.AddTestAuthentication();          // header-driven ClaimsPrincipal, no Authentik
        });
    }
}
```

### Integration test rules

- Real PostgreSQL via Testcontainers. Never mock your own database. Never mock the dispatcher.
- Mock only external services (SMTP, Authentik admin API, HTTP clients).
- Auth: a test authentication handler that reads the subject from a header; each test creates
  the client for the identity it needs. Never share one "current user" across tests.
- Isolation: unique subjects / IDs per test. Truncate tables in the fixture when a test needs an
  empty world.
- Assert observable behaviour (status, body, DB row via a query), never SQL text or log output.
- Cross-module tests (e.g. shopping list + household) spin up **one container per module DB**.
- Locally, `WebApplicationFactory` opens file watchers; `scripts/verify.sh` sets
  `DOTNET_USE_POLLING_FILE_WATCHER=1` so the inotify limit is not hit.

## Test Data Builders

```csharp
internal sealed class HouseholdBuilder
{
    private HouseholdId _id = HouseholdId.New();
    private string _name = "Home";
    private PersonId _owner = PersonId.New();
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public HouseholdBuilder WithId(HouseholdId id) { _id = id; return this; }
    public HouseholdBuilder WithName(string name) { _name = name; return this; }
    public HouseholdBuilder WithOwner(PersonId owner) { _owner = owner; return this; }
    public HouseholdBuilder At(DateTimeOffset now) { _now = now; return this; }

    public Household Build() => Household.Create(_id, _name, _owner, _now);
}
```

Builders live in `<Module>.UnitTests/Builders/` and are shared with the integration project
via `InternalsVisibleTo` or a linked file — never duplicated.

## What is not a test

- A test that only checks a mock was called with anything (`Arg.Any` everywhere).
- A `[Fact]` with `Skip` to get CI green.
- `Task.Delay` / `Thread.Sleep` to wait for a worker — drive the worker directly
  (`RunOnceAsync`) or advance `FakeTimeProvider`.
