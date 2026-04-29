namespace Notifications.UnitTests.EventHandlers;

using DietPlanner.Contracts.Events;
#pragma warning disable IDE0005
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
#pragma warning restore IDE0005
using Notifications.Domain.ValueObjects;

public sealed class WaterReminderDueIntegrationEventHandlerTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly WaterReminderDueIntegrationEventHandler _sut;

    public WaterReminderDueIntegrationEventHandlerTests()
        => _sut = new WaterReminderDueIntegrationEventHandler(_dispatcher);

    [Fact]
    public async Task HandleAsync_DispatchesWaterReminderTypeWithCorrectUserAndLocale()
    {
        var @event = new WaterReminderDueIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
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

    [Fact]
    public async Task HandleAsync_WhenEventIsNull_Throws()
    {
        var act = async () => await _sut.HandleAsync(null!, CancellationToken.None);
        await act.ShouldThrowAsync<ArgumentNullException>();
    }
}
