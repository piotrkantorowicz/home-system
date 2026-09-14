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

[Collection(nameof(PostgresCollectionDefinition))]
public sealed class DomainEventToOutboxIntegrationTests : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private ServiceProvider _sp = default!;

    public DomainEventToOutboxIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

    // ---- domain types ----

    public sealed record TestAggregateCreatedDomainEvent(Guid AggregateId) : IDomainEvent;

    public sealed class TestAggregate : AggregateRoot<Guid>
    {
        private TestAggregate() { }

        public static TestAggregate Create(Guid id)
        {
            var a = new TestAggregate { Id = id };
            a.RaiseDomainEvent(new TestAggregateCreatedDomainEvent(id));
            return a;
        }
    }

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

    public async Task DisposeAsync() => await _sp.DisposeAsync();

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
