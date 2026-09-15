namespace Notifications.UnitTests.EventHandlers;

using DietPlanner.Contracts.Events;
#pragma warning disable IDE0005
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
#pragma warning restore IDE0005
using Notifications.Domain.ValueObjects;

/// <summary>Unit tests for <c>WaterReminderDueIntegrationEventHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class WaterReminderDueIntegrationEventHandlerTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly WaterReminderDueIntegrationEventHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public WaterReminderDueIntegrationEventHandlerTests()
        => _sut = new WaterReminderDueIntegrationEventHandler(_dispatcher);

    /// <summary><c>HandleAsync</c> dispatches water reminder type with correct user and locale.</summary>
    [Fact]
    public async Task HandleAsync_DispatchesWaterReminderTypeWithCorrectUserAndLocale()
    {
        var @event = new WaterReminderDueIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: TestClock.UtcNow,
            UserId: "u42",
            Locale: "pl");

        await _sut.HandleAsync(@event, CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            NotificationType.WaterReminder,
            "u42",
            "pl",
            Arg.Is<string>(p => p.Contains("\"UserId\"") && p.Contains("u42")),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>When event is null: <c>HandleAsync</c> throws.</summary>
    [Fact]
    public async Task HandleAsync_WhenEventIsNull_Throws()
    {
        var act = async () => await _sut.HandleAsync(null!, CancellationToken.None);
        await act.ShouldThrowAsync<ArgumentNullException>();
    }
}
