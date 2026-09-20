namespace Notifications.UnitTests.EventHandlers;

using System.Globalization;
using DietPlanner.Contracts.Events;
#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
#pragma warning restore IDE0005
using Notifications.Domain.ValueObjects;

/// <summary>Unit tests for <c>MealMissedIntegrationEventHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class MealMissedIntegrationEventHandlerTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly MealMissedIntegrationEventHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public MealMissedIntegrationEventHandlerTests()
        => _sut = new MealMissedIntegrationEventHandler(_dispatcher);

    /// <summary><c>HandleAsync</c> dispatches meal missed with formatted placeholders.</summary>
    [Fact]
    public async Task HandleAsync_DispatchesMealMissedWithFormattedPlaceholders()
    {
        var planned = new DateTime(2026, 4, 28, 8, 0, 0, DateTimeKind.Utc);
        var @event = new MealMissedIntegrationEvent(
            EventId: Guid.NewGuid(), OccurredAt: planned.AddHours(1),
            UserId: "u2", Locale: "en",
            MealEntryId: Guid.NewGuid(), MealSlotName: "Breakfast", PlannedAt: planned);

        await _sut.HandleAsync(@event, TestContext.Current.CancellationToken);

        await _dispatcher.Received(1).DispatchAsync(
            NotificationType.MealMissed,
            "u2",
            "en",
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d["MealSlotName"] == "Breakfast" && d["PlannedAt"] == "08:00"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Under culture with dot time separator: <c>HandleAsync</c> keeps colon in planned at.</summary>
    [Fact]
    public async Task HandleAsync_UnderCultureWithDotTimeSeparator_KeepsColonInPlannedAt()
    {
        // fi-FI formats "HH:mm" as "08.00" when the culture is applied; the placeholder must
        // stay invariant because the template wording expects "08:00".
        var planned = new DateTime(2026, 4, 28, 8, 0, 0, DateTimeKind.Utc);
        var @event = new MealMissedIntegrationEvent(
            EventId: Guid.NewGuid(), OccurredAt: planned.AddHours(1),
            UserId: "u2", Locale: "fi",
            MealEntryId: Guid.NewGuid(), MealSlotName: "Aamiainen", PlannedAt: planned);

        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fi-FI");
        try
        {
            await _sut.HandleAsync(@event, TestContext.Current.CancellationToken);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        await _dispatcher.Received(1).DispatchAsync(
            NotificationType.MealMissed,
            "u2",
            "fi",
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d => d["PlannedAt"] == "08:00"),
            Arg.Any<CancellationToken>());
    }
}
