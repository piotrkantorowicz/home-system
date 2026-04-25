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

    public sealed class HelloReceiver
    {
        public List<HelloIntegrationEvent> Received { get; } = new();
    }

    public sealed class HelloHandler : IIntegrationEventHandler<HelloIntegrationEvent>
    {
        private readonly HelloReceiver _receiver;

        public HelloHandler(HelloReceiver receiver) => _receiver = receiver;

        public Task HandleAsync(HelloIntegrationEvent @event, CancellationToken ct = default)
        {
            _receiver.Received.Add(@event);
            return Task.CompletedTask;
        }
    }

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddDbContext<MessagingTestDbContext>(o => o.UseNpgsql(_fixture.ConnectionString));
        services.AddIntegrationEventBus().UseInProcessTransport();
        services.AddOutbox<MessagingTestDbContext>();

        services.AddSingleton<HelloReceiver>();
        services.AddIntegrationEventConsumer<HelloIntegrationEvent, HelloHandler, MessagingTestDbContext>();

        services.AddLogging();

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
        await using (var scope = _sp.CreateAsyncScope())
        {
            var bus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();
            var db = scope.ServiceProvider.GetRequiredService<MessagingTestDbContext>();

            await bus.PublishAsync(
                new HelloIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "hi"),
                default);
            await db.SaveChangesAsync();
        }

        var worker = new OutboxWorker<MessagingTestDbContext>(
            _sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxWorkerOptions()),
            NullLogger<OutboxWorker<MessagingTestDbContext>>.Instance);

        await worker.RunOnceAsync(default);

        var receiver = _sp.GetRequiredService<HelloReceiver>();
        receiver.Received.Count.ShouldBe(1);
        receiver.Received[0].Greeting.ShouldBe("hi");
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

        var worker = new OutboxWorker<MessagingTestDbContext>(
            _sp.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxWorkerOptions()),
            NullLogger<OutboxWorker<MessagingTestDbContext>>.Instance);
        await worker.RunOnceAsync(default);

        var receiver = _sp.GetRequiredService<HelloReceiver>();
        receiver.Received.Count.ShouldBe(1);
    }
}
