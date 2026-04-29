namespace Notifications.UnitTests.EventHandlers;

using DietPlanner.Contracts.Events;
#pragma warning disable IDE0005
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
#pragma warning restore IDE0005
using Notifications.Domain.ValueObjects;

public sealed class MealReminderDueIntegrationEventHandlerTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly MealReminderDueIntegrationEventHandler _sut;

    public MealReminderDueIntegrationEventHandlerTests()
        => _sut = new MealReminderDueIntegrationEventHandler(_dispatcher);

    [Fact]
    public async Task HandleAsync_DispatchesMealReminderWithFormattedPlaceholders()
    {
        var planned = new DateTime(2026, 4, 28, 12, 0, 0, DateTimeKind.Utc);
        var @event = new MealReminderDueIntegrationEvent(
            EventId: Guid.NewGuid(), OccurredAt: planned.AddMinutes(-10),
            UserId: "u1", Locale: "en",
            MealEntryId: Guid.NewGuid(), MealSlotName: "Lunch", PlannedAt: planned);

        await _sut.HandleAsync(@event, CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            NotificationType.MealReminder,
            "u1",
            "en",
            Arg.Is<string>(p => p.Contains("\"MealEntryId\"") && p.Contains("Lunch")),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d["MealSlotName"] == "Lunch" && d["PlannedAt"] == "12:00"),
            Arg.Any<CancellationToken>());
    }
}
