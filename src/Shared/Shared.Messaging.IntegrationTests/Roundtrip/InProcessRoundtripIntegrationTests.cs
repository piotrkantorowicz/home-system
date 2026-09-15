namespace Shared.Messaging.IntegrationTests.Roundtrip;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Ef.Extensions;
using Shared.Infrastructure.Messaging.Extensions;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Messaging.IntegrationTests.Fixtures;
using Shouldly;
using Xunit;

/// <summary>Integration tests for <c>InProcessRoundtrip</c> against a real PostgreSQL container.</summary>
[Collection(nameof(PostgresCollectionDefinition))]
public sealed class InProcessRoundtripIntegrationTests : IAsyncLifetime
{
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 9, 12, 10, 0, 0, TimeSpan.Zero));

    private readonly PostgresContainerFixture _fixture;
    private ServiceProvider _sp = default!;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public InProcessRoundtripIntegrationTests(PostgresContainerFixture fixture) => _fixture = fixture;

    /// <summary>Integration event pushed through outbox → worker → transport → inbox in the round-trip test.</summary>
    /// <param name="EventId">Unique identity of the event.</param>
    /// <param name="OccurredAt">When it was published, UTC.</param>
    /// <param name="Greeting">Payload asserted on the receiving side.</param>
    public sealed record HelloIntegrationEvent(Guid EventId, DateTime OccurredAt, string Greeting)
        : IIntegrationEvent;

    /// <summary>Singleton sink the handler appends to, so the test can observe deliveries.</summary>
    public sealed class HelloReceiver
    {
        /// <summary>Every event the handler was invoked with, in order.</summary>
        public List<HelloIntegrationEvent> Received { get; } = new();
    }

    /// <summary>Consumer that records each delivery in <see cref="HelloReceiver"/>.</summary>
    public sealed class HelloHandler : IIntegrationEventHandler<HelloIntegrationEvent>
    {
        private readonly HelloReceiver _receiver;

        /// <summary>Creates the handler.</summary>
        /// <param name="receiver">The shared sink to record into.</param>
        public HelloHandler(HelloReceiver receiver) => _receiver = receiver;

        /// <inheritdoc />
        public Task HandleAsync(HelloIntegrationEvent @event, CancellationToken ct = default)
        {
            _receiver.Received.Add(@event);
            return Task.CompletedTask;
        }
    }

    /// <summary>Builds a service provider with the full in-process messaging stack against a fresh schema.</summary>
    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddDbContext<MessagingTestDbContext>(o => o.UseNpgsql(_fixture.ConnectionString));
        services.AddIntegrationEventBus().UseInProcessTransport();
        services.AddOutbox<MessagingTestDbContext>();

        services.AddSingleton<HelloReceiver>();
        services.AddIntegrationEventConsumer<HelloIntegrationEvent, HelloHandler, MessagingTestDbContext>();

        services.AddLogging();
        services.AddSingleton<TimeProvider>(Clock);

        _sp = services.BuildServiceProvider();

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MessagingTestDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    /// <summary>Disposes the service provider and its <c>DbContext</c>.</summary>
    public async Task DisposeAsync() => await _sp.DisposeAsync();

    /// <summary><c>Publish</c> then run once and handler receives event.</summary>
    [Fact]
    public async Task Publish_ThenRunOnce_HandlerReceivesEvent()
    {
        await using (var scope = _sp.CreateAsyncScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();
            var db = scope.ServiceProvider.GetRequiredService<MessagingTestDbContext>();

            await bus.PublishAsync(
                new HelloIntegrationEvent(Guid.NewGuid(), Clock.GetUtcNow().UtcDateTime, "hi"),
                default);
            await db.SaveChangesAsync();
        }

        var worker = new OutboxWorker<MessagingTestDbContext>(
            _sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxWorkerOptions()),
            NullLogger<OutboxWorker<MessagingTestDbContext>>.Instance,
            Clock);

        await worker.RunOnceAsync(default);

        var receiver = _sp.GetRequiredService<HelloReceiver>();
        receiver.Received.Count.ShouldBe(1);
        receiver.Received[0].Greeting.ShouldBe("hi");
    }

    /// <summary>Same event id twice: <c>Publish</c> handler only receives once.</summary>
    [Fact]
    public async Task Publish_SameEventIdTwice_HandlerOnlyReceivesOnce()
    {
        var eventId = Guid.NewGuid();

        async Task PublishAsync()
        {
            await using var scope = _sp.CreateAsyncScope();
            var bus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();
            var db = scope.ServiceProvider.GetRequiredService<MessagingTestDbContext>();
            await bus.PublishAsync(new HelloIntegrationEvent(eventId, Clock.GetUtcNow().UtcDateTime, "hi"), default);
            await db.SaveChangesAsync();
        }

        await PublishAsync();
        await PublishAsync();

        var worker = new OutboxWorker<MessagingTestDbContext>(
            _sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxWorkerOptions()),
            NullLogger<OutboxWorker<MessagingTestDbContext>>.Instance,
            Clock);
        await worker.RunOnceAsync(default);

        var receiver = _sp.GetRequiredService<HelloReceiver>();
        receiver.Received.Count.ShouldBe(1);
    }
}
