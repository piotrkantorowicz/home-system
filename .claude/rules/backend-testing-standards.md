# Backend — Testing Standards

> **Assertion library:** Shouldly only. Project tech stack: xUnit + Shouldly + NSubstitute.
> FluentAssertions is intentionally not on the package list — do not add it.

## Test Project Layout

```
tests/
  BudgetPlan.UnitTests/
    Domain/
      BudgetPlanTests.cs
      BudgetEntryTests.cs
      MoneyTests.cs
    Application/
      Commands/
        CreateBudgetPlanCommandHandlerTests.cs
      Queries/
        GetBudgetPlanQueryHandlerTests.cs
  BudgetPlan.IntegrationTests/
    Api/
      BudgetPlanEndpointsTests.cs
    Persistence/
      BudgetPlanRepositoryTests.cs
```

## Unit Tests — Domain Layer

Test domain logic through aggregate methods. No infrastructure, no mocks of your own domain.

```csharp
public sealed class BudgetPlanTests
{
    // Naming: MethodName_StateUnderTest_ExpectedBehavior
    [Fact]
    public void AddEntry_WhenAmountExceedsLimit_ThrowsDomainException()
    {
        // Arrange
        var plan = BudgetPlan.Create(
            BudgetPlanId.New(),
            new Money(100, "PLN"),
            DateRange.CurrentMonth());

        // Act
        var act = () => plan.AddEntry(new Money(150, "PLN"), "Over-limit expense");

        // Assert
        act.ShouldThrow<BudgetPlanDomainException>()
           .Message.ShouldContain("limit");
    }

    [Fact]
    public void AddEntry_WhenAmountIsWithinLimit_RaisesBudgetEntryAddedDomainEvent()
    {
        // Arrange
        var plan = BudgetPlan.Create(
            BudgetPlanId.New(),
            new Money(500, "PLN"),
            DateRange.CurrentMonth());

        // Act
        plan.AddEntry(new Money(100, "PLN"), "Coffee");

        // Assert
        plan.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<BudgetEntryAddedDomainEvent>();
        plan.Entries.Count.ShouldBe(1);
    }

    [Fact]
    public void Create_Always_RaisesBudgetPlanCreatedDomainEvent()
    {
        // Act
        var plan = BudgetPlan.Create(BudgetPlanId.New(), new Money(1000, "PLN"), DateRange.CurrentMonth());

        // Assert
        plan.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<BudgetPlanCreatedDomainEvent>();
    }
}
```

## Unit Tests — Application Layer (Handlers)

Mock only infrastructure boundaries (repository, unit of work, external services).

```csharp
public sealed class CreateBudgetPlanCommandHandlerTests
{
    private readonly IBudgetPlanRepository _repository = Substitute.For<IBudgetPlanRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateBudgetPlanCommandHandler _sut;

    public CreateBudgetPlanCommandHandlerTests()
        => _sut = new CreateBudgetPlanCommandHandler(_repository, _unitOfWork);

    [Fact]
    public async Task Handle_WithValidCommand_AddsPlanAndCommits()
    {
        // Arrange
        var command = new CreateBudgetPlanCommand(Guid.NewGuid(), 500m, "PLN");

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<BudgetPlan>(p => p.Limit == new Money(500m, "PLN")),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
```

### Unit Test Rules
- No database, no filesystem, no network — everything outside the unit under test is mocked.
- Use NSubstitute for mocks (`Substitute.For<T>()`).
- Use **Shouldly** for readable assertions (`.ShouldBe`, `.ShouldThrow<T>`, `.ShouldNotBeNull`, `.ShouldHaveSingleItem`, `.ShouldBeOfType<T>`) — no plain `Assert.Equal`, no FluentAssertions.
- Test **one behaviour** per test method.
- Parameterize with `[Theory] + [InlineData]` for value variations, not for different scenarios.

## Integration Tests — API Layer

Test the full vertical slice: HTTP request → endpoint → handler → real database → response.

```csharp
// Uses WebApplicationFactory + TestContainers for PostgreSQL
public sealed class BudgetPlanEndpointsTests : IClassFixture<BudgetPlanWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly BudgetPlanWebApplicationFactory _factory;

    public BudgetPlanEndpointsTests(BudgetPlanWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_BudgetPlans_WithValidRequest_Returns201()
    {
        // Arrange
        var request = new CreateBudgetPlanRequest(Guid.NewGuid(), 500m, "PLN");

        // Act
        var response = await _client.PostAsJsonAsync("/api/budget-plans", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
    }

    [Fact]
    public async Task GET_BudgetPlan_WhenNotFound_Returns404()
    {
        // Act
        var response = await _client.GetAsync($"/api/budget-plans/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
```

```csharp
// Shared test infrastructure
public sealed class BudgetPlanWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("budgetplan_test")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace the real connection string with the test container's
            services.RemoveAll<DbContextOptions<BudgetPlanDbContext>>();
            services.AddDbContext<BudgetPlanDbContext>(opts =>
                opts.UseNpgsql(_postgres.GetConnectionString()));
        });
    }
}
```

### Integration Test Rules
- Use a **real PostgreSQL database** via TestContainers — never mock your own database.
- Mock only **external services** (HTTP clients, email senders, payment gateways).
- Reset database state between tests: either use a transaction-per-test that's rolled back, or
  truncate affected tables in a fixture's `InitializeAsync`.
- Never assert on exact SQL queries — assert on observable behavior (HTTP response, returned DTO).

## Test Data Builders

Use the builder pattern for complex aggregates instead of repeating construction:

```csharp
internal sealed class BudgetPlanBuilder
{
    private BudgetPlanId _id = BudgetPlanId.New();
    private Money _limit = new(1000m, "PLN");
    private DateRange _period = DateRange.CurrentMonth();

    public BudgetPlanBuilder WithId(BudgetPlanId id) { _id = id; return this; }
    public BudgetPlanBuilder WithLimit(decimal value, string currency) { _limit = new(value, currency); return this; }
    public BudgetPlanBuilder WithPeriod(DateRange period) { _period = period; return this; }

    public BudgetPlan Build() => BudgetPlan.Create(_id, _limit, _period);
}

// Usage in tests
var plan = new BudgetPlanBuilder()
    .WithLimit(200m, "PLN")
    .Build();
```
