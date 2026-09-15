namespace Notifications.UnitTests.EventHandlers;

using DietPlanner.Contracts.Events;
#pragma warning disable IDE0005
using Notifications.Application.Dispatching;
using Notifications.Application.EventHandlers;
#pragma warning restore IDE0005
using Notifications.Domain.ValueObjects;

/// <summary>Unit tests for <c>GoalMilestoneReachedIntegrationEventHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class GoalMilestoneReachedIntegrationEventHandlerTests
{
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly GoalMilestoneReachedIntegrationEventHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public GoalMilestoneReachedIntegrationEventHandlerTests()
        => _sut = new GoalMilestoneReachedIntegrationEventHandler(_dispatcher);

    /// <summary><c>HandleAsync</c> dispatches goal milestone with label placeholder.</summary>
    [Fact]
    public async Task HandleAsync_DispatchesGoalMilestoneWithLabelPlaceholder()
    {
        var @event = new GoalMilestoneReachedIntegrationEvent(
            EventId: Guid.NewGuid(), OccurredAt: TestClock.UtcNow,
            UserId: "u3", Locale: "en",
            GoalKind: "WeightTarget",
            MilestoneLabel: "Reached target weight of 70 kg",
            Value: 70m);

        await _sut.HandleAsync(@event, CancellationToken.None);

        await _dispatcher.Received(1).DispatchAsync(
            NotificationType.GoalMilestone,
            "u3",
            "en",
            Arg.Any<string>(),
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d["MilestoneLabel"] == "Reached target weight of 70 kg"),
            Arg.Any<CancellationToken>());
    }
}
