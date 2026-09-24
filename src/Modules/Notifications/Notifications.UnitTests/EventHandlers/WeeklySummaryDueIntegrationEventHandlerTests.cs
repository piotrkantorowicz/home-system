namespace Notifications.UnitTests.EventHandlers;

using DietPlanner.Contracts.Events;
#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
#pragma warning restore IDE0005
using Notifications.Domain.ValueObjects;

/// <summary>Unit tests for <c>WeeklySummaryDueIntegrationEventHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class WeeklySummaryDueIntegrationEventHandlerTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly WeeklySummaryDueIntegrationEventHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public WeeklySummaryDueIntegrationEventHandlerTests()
        => _sut = new WeeklySummaryDueIntegrationEventHandler(_dispatcher);

    /// <summary>With valid event: <c>HandleAsync</c> dispatches weekly summary notification.</summary>
    [Fact]
    public async Task HandleAsync_WithValidEvent_DispatchesWeeklySummaryNotification()
    {
        var weekStart = new DateOnly(2026, 4, 20);
        var weekEnd = new DateOnly(2026, 4, 26);

        var @event = new WeeklySummaryDueIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: TestClock.UtcNow,
            UserId: "u99",
            Locale: "en",
            WeekStart: weekStart,
            WeekEnd: weekEnd,
            TotalKcal: 14000,
            TargetKcal: 15000,
            AvgWaterLiters: 2.1m,
            WeightDeltaKg: -0.5m,
            MealsCompleted: 19,
            MealsPlanned: 21);

        await _sut.HandleAsync(@event, TestContext.Current.CancellationToken);

        await _dispatcher.Received(1).DispatchAsync(
            NotificationType.WeeklySummary,
            "u99",
            "en",
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d.ContainsKey("TotalKcal") &&
                d.ContainsKey("TargetKcal") &&
                d.ContainsKey("MealsCompleted") &&
                d.ContainsKey("MealsPlanned")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>With null event: <c>HandleAsync</c> throws.</summary>
    [Fact]
    public async Task HandleAsync_WithNullEvent_Throws()
    {
        var act = async () => await _sut.HandleAsync(null!, TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    /// <summary>With null event: <c>HandleAsync</c> does not call dispatcher.</summary>
    [Fact]
    public async Task HandleAsync_WithNullEvent_DoesNotCallDispatcher()
    {
        try
        {
            await _sut.HandleAsync(null!, TestContext.Current.CancellationToken);
        }
        catch (ArgumentNullException)
        {
            // expected
        }

        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default, default!, default!, default!, default!, TestContext.Current.CancellationToken);
    }
}
