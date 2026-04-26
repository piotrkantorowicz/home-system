namespace DietPlanner.UnitTests.Application.EventHandlers;

#pragma warning disable IDE0005 // false positive — InternalsVisibleTo prevents Roslyn from resolving internal types
using DietPlanner.Application.EventHandlers;
#pragma warning restore IDE0005
using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Events;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Messaging;

public sealed class GoalMilestoneEvaluatorTests
{
    private readonly IUserGoalRepository _userGoalRepository = Substitute.For<IUserGoalRepository>();
    private readonly IIntegrationEventBus _bus = Substitute.For<IIntegrationEventBus>();
    private readonly GoalMilestoneEvaluator _sut;

    public GoalMilestoneEvaluatorTests()
        => _sut = new GoalMilestoneEvaluator(_userGoalRepository, _bus);

    [Fact]
    public async Task HandleAsync_WhenNoGoalExists_DoesNothing()
    {
        _userGoalRepository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns((UserGoal?)null);

        await _sut.HandleAsync(new WeightEntryAddedDomainEvent("user-1", 70m, DateOnly.FromDateTime(DateTime.UtcNow)), CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<GoalMilestoneReachedIntegrationEvent>(),
            Arg.Any<CancellationToken>());
        _userGoalRepository.DidNotReceive().Update(Arg.Any<UserGoal>());
    }

    [Fact]
    public async Task HandleAsync_WhenGoalHasNoTarget_DoesNothing()
    {
        var goal = UserGoal.Create(UserGoalId.New(), "user-1", 2000, null, null, null, null);
        _userGoalRepository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(goal);

        await _sut.HandleAsync(new WeightEntryAddedDomainEvent("user-1", 70m, DateOnly.FromDateTime(DateTime.UtcNow)), CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<GoalMilestoneReachedIntegrationEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenWeightAboveTarget_DoesNotEmit()
    {
        var goal = UserGoal.Create(
            UserGoalId.New(), "user-1", 2000, null, null, null, null,
            targetWeightKg: 70m);
        _userGoalRepository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(goal);

        await _sut.HandleAsync(new WeightEntryAddedDomainEvent("user-1", 75m, DateOnly.FromDateTime(DateTime.UtcNow)), CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<GoalMilestoneReachedIntegrationEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenTargetCrossed_PublishesEventAndMarksAchieved()
    {
        var goal = UserGoal.Create(
            UserGoalId.New(), "user-1", 2000, null, null, null, null,
            targetWeightKg: 70m);
        _userGoalRepository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(goal);

        await _sut.HandleAsync(new WeightEntryAddedDomainEvent("user-1", 69.5m, DateOnly.FromDateTime(DateTime.UtcNow)), CancellationToken.None);

        await _bus.Received(1).PublishAsync(
            Arg.Is<GoalMilestoneReachedIntegrationEvent>(e =>
                e.UserId == "user-1" &&
                e.GoalKind == "WeightTarget" &&
                e.Value == 70m &&
                e.MilestoneLabel.Contains("70")),
            Arg.Any<CancellationToken>());

        _userGoalRepository.Received(1).Update(Arg.Is<UserGoal>(g => g.MilestoneAchievedAt != null));
        goal.MilestoneAchievedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenTargetAlreadyAchieved_DoesNotEmitAgain()
    {
        var goal = UserGoal.Create(
            UserGoalId.New(), "user-1", 2000, null, null, null, null,
            targetWeightKg: 70m);
        goal.MarkMilestoneAchieved(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _userGoalRepository.GetByUserIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(goal);

        await _sut.HandleAsync(new WeightEntryAddedDomainEvent("user-1", 65m, DateOnly.FromDateTime(DateTime.UtcNow)), CancellationToken.None);

        await _bus.DidNotReceive().PublishAsync(
            Arg.Any<GoalMilestoneReachedIntegrationEvent>(),
            Arg.Any<CancellationToken>());
    }
}
