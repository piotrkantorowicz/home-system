namespace Notifications.UnitTests.EventHandlers;

using DietPlanner.Contracts.Events;
#pragma warning disable IDE0005
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
#pragma warning restore IDE0005
using Notifications.Domain.ValueObjects;

public sealed class MealMissedIntegrationEventHandlerTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly MealMissedIntegrationEventHandler _sut;

    public MealMissedIntegrationEventHandlerTests()
        => _sut = new MealMissedIntegrationEventHandler(_dispatcher);

    [Fact]
    public async Task HandleAsync_DispatchesMealMissedWithFormattedPlaceholders()
    {
        var planned = new DateTime(2026, 4, 28, 8, 0, 0, DateTimeKind.Utc);
        var @event = new MealMissedIntegrationEvent(
            EventId: Guid.NewGuid(), OccurredAt: planned.AddHours(1),
            UserId: "u2", Locale: "en",
            MealEntryId: Guid.NewGuid(), MealSlotName: "Breakfast", PlannedAt: planned);

        await _sut.HandleAsync(@event, CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            NotificationType.MealMissed,
            "u2",
            "en",
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d["MealSlotName"] == "Breakfast" && d["PlannedAt"] == "08:00"),
            Arg.Any<CancellationToken>());
    }
}
