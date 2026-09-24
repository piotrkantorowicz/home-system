namespace Notifications.UnitTests.EventHandlers;

using DietPlanner.Contracts.Events;
#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
#pragma warning restore IDE0005
using Notifications.Domain.ValueObjects;

/// <summary>Unit tests for <c>MealReminderDueIntegrationEventHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class MealReminderDueIntegrationEventHandlerTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly MealReminderDueIntegrationEventHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public MealReminderDueIntegrationEventHandlerTests()
        => _sut = new MealReminderDueIntegrationEventHandler(_dispatcher);

    /// <summary><c>HandleAsync</c> dispatches meal reminder with formatted placeholders.</summary>
    [Fact]
    public async Task HandleAsync_DispatchesMealReminderWithFormattedPlaceholders()
    {
        var planned = new DateTime(2026, 4, 28, 12, 0, 0, DateTimeKind.Utc);
        var @event = new MealReminderDueIntegrationEvent(
            EventId: Guid.NewGuid(), OccurredAt: planned.AddMinutes(-10),
            UserId: "u1", Locale: "en",
            MealEntryId: Guid.NewGuid(), MealSlotName: "Lunch", PlannedAt: planned);

        await _sut.HandleAsync(@event, TestContext.Current.CancellationToken);

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
