namespace Shared.Messaging.Tests.Outbox;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using NSubstitute;
using Shared.Abstractions.Messaging;
using Shared.Infrastructure.Messaging.Outbox;
using Shared.Infrastructure.Messaging.Telemetry;
using Shouldly;

/// <summary>Unit tests for the inbox metric recorded by <c>InboxAwareHandler</c>.</summary>
public sealed class InboxAwareHandlerTests
{
    /// <summary>Event handled by the wrapped handler.</summary>
    /// <param name="EventId">Idempotency key.</param>
    /// <param name="OccurredAt">When the event happened.</param>
    public sealed record PingEvent(Guid EventId, DateTime OccurredAt) : IIntegrationEvent;

    /// <summary>Shutdown cancellation is not a handler failure, so no inbox measurement is recorded.</summary>
    [Fact]
    public async Task HandleAsync_WhenCancelledDuringShutdown_RecordsNothing()
    {
        var meters = new ServiceCollection().AddMetrics().BuildServiceProvider()
            .GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>();
        using var duration = new MetricCollector<double>(meters, MessagingMetrics.MeterName, "homesystem.inbox.handle.duration");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var inbox = Substitute.For<IInboxExecutor>();
        inbox.ExecuteAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled(cts.Token));
        var sut = new InboxAwareHandler<PingEvent>(
            Substitute.For<IIntegrationEventHandler<PingEvent>>(), inbox, new MessagingMetrics(meters));

        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.HandleAsync(new PingEvent(Guid.CreateVersion7(), DateTime.UnixEpoch), cts.Token));

        duration.GetMeasurementSnapshot().ShouldBeEmpty();
    }
}
