namespace Notifications.UnitTests.EventHandlers;

using DietPlanner.Contracts.Events;
#pragma warning disable IDE0005
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
#pragma warning restore IDE0005
using Notifications.Domain.ValueObjects;

public sealed class WeeklySummaryDueIntegrationEventHandlerTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly WeeklySummaryDueIntegrationEventHandler _sut;

    public WeeklySummaryDueIntegrationEventHandlerTests()
        => _sut = new WeeklySummaryDueIntegrationEventHandler(_dispatcher);

    [Fact]
    public async Task HandleAsync_WithValidEvent_DispatchesWeeklySummaryNotification()
    {
        var weekStart = new DateOnly(2026, 4, 20);
        var weekEnd = new DateOnly(2026, 4, 26);

        var @event = new WeeklySummaryDueIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
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

        await _sut.HandleAsync(@event, CancellationToken.None);

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

    [Fact]
    public async Task HandleAsync_WithNullEvent_Throws()
    {
        var act = async () => await _sut.HandleAsync(null!, CancellationToken.None);
        await act.ShouldThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task HandleAsync_WithNullEvent_DoesNotCallDispatcher()
    {
        try
        {
            await _sut.HandleAsync(null!, CancellationToken.None);
        }
        catch (ArgumentNullException)
        {
            // expected
        }

        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(
            default, default!, default!, default!, default!, default);
    }
}
