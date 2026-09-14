namespace Shared.Messaging.IntegrationTests.Roundtrip;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Ef.Extensions;
using Shared.Infrastructure.Messaging.Ef.Inbox;
using Shared.Infrastructure.Messaging.Ef.Outbox;
using Shared.Infrastructure.Messaging.Extensions;
using Shared.Infrastructure.Persistence;
using Shared.Messaging.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

/// <summary>Integration tests for <c>DomainEventToOutbox</c> against a real PostgreSQL container.</summary>
[Collection(nameof(PostgresCollectionDefinition))]
public sealed class DomainEventToOutboxIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private ServiceProvider _sp = default!;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public DomainEventToOutboxIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

    // ---- domain types ----

    /// <summary>Domain event raised by <see cref="TestAggregate.Create"/>.</summary>
    /// <param name="AggregateId">The aggregate that was created.</param>
    public sealed record TestAggregateCreatedDomainEvent(Guid AggregateId) : IDomainEvent;

    /// <summary>Minimal aggregate whose creation raises a domain event, to drive the interceptor → handler → outbox path.</summary>
    public sealed class TestAggregate : AggregateRoot<Guid>
    {
        private TestAggregate() { }

        /// <summary>Creates the aggregate and raises <see cref="TestAggregateCreatedDomainEvent"/>.</summary>
        /// <param name="id">Identifier for the new aggregate.</param>
        public static TestAggregate Create(Guid id)
        {
            var a = new TestAggregate { Id = id };
            a.RaiseDomainEvent(new TestAggregateCreatedDomainEvent(id));
            return a;
        }
    }

    /// <summary>Integration event the test domain-event handler publishes to the outbox.</summary>
    /// <param name="EventId">Unique identity of the event.</param>
    /// <param name="OccurredAt">When it was published, UTC.</param>
    /// <param name="AggregateId">The aggregate it describes.</param>
    public sealed record TestCreatedIntegrationEvent(Guid EventId, DateTime OccurredAt, Guid AggregateId)
        : IIntegrationEvent;

    internal sealed class CreatedDomainEventHandler : IDomainEventHandler<TestAggregateCreatedDomainEvent>
    {
        private readonly IIntegrationEventBus _bus;

        public CreatedDomainEventHandler(IIntegrationEventBus bus) => _bus = bus;

        public Task HandleAsync(TestAggregateCreatedDomainEvent domainEvent, CancellationToken ct = default)
            => _bus.PublishAsync(
                new TestCreatedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, domainEvent.AggregateId),
                ct);
    }

    // ---- DbContext ----

    internal sealed class DomainEventTestDbContext : DbContext
    {
        public DomainEventTestDbContext(DbContextOptions<DomainEventTestDbContext> options) : base(options) { }

        public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregate>(b =>
            {
                b.ToTable("test_aggregates");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).HasColumnName("id");
                b.Ignore(x => x.DomainEvents);
            });
            modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
            modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration());
            base.OnModelCreating(modelBuilder);
        }
    }

    /// <summary>Builds a service provider with the outbox, dispatcher interceptor and test handler wired to a fresh schema.</summary>
    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddScoped<DomainEventDispatcherInterceptor>();
        services.AddDbContext<DomainEventTestDbContext>((sp, o) =>
            o.UseNpgsql(_fixture.ConnectionString)
             .AddInterceptors(sp.GetRequiredService<DomainEventDispatcherInterceptor>()));

        services.AddIntegrationEventBus().UseInProcessTransport();
        services.AddOutbox<DomainEventTestDbContext>();

        services.AddScoped<IDomainEventHandler<TestAggregateCreatedDomainEvent>, CreatedDomainEventHandler>();

        services.AddLogging();

        _sp = services.BuildServiceProvider();

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DomainEventTestDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    /// <summary>Disposes the service provider and its <c>DbContext</c>.</summary>
    public async Task DisposeAsync() => await _sp.DisposeAsync();

    /// <summary>When aggregate raises domain event: <c>SaveChanges</c> persists outbox row atomically.</summary>
    [Fact]
    public async Task SaveChanges_WhenAggregateRaisesDomainEvent_PersistsOutboxRowAtomically()
    {
        var aggregateId = Guid.NewGuid();

        await using (var scope = _sp.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DomainEventTestDbContext>();
            var aggregate = TestAggregate.Create(aggregateId);
            await db.Aggregates.AddAsync(aggregate);
            await db.SaveChangesAsync();
        }

        // Verify both rows exist after the same SaveChangesAsync.
        await using var verifyScope = _sp.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DomainEventTestDbContext>();

        bool aggregateExists = await verifyDb.Aggregates.AnyAsync(a => a.Id == aggregateId);
        aggregateExists.ShouldBeTrue();

        List<OutboxMessageEntity> outboxRows = await verifyDb.Set<OutboxMessageEntity>().ToListAsync();
        outboxRows.Count.ShouldBe(1);
        outboxRows[0].EventType.ShouldContain(nameof(TestCreatedIntegrationEvent));
    }
}
