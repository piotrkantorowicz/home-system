namespace DietPlanner.Application.EventHandlers;

using DietPlanner.Contracts.Events;
using DietPlanner.Domain.Events;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Messaging;

internal sealed class GoalMilestoneEvaluator(
    IUserGoalRepository userGoalRepository,
    IIntegrationEventBus integrationEventBus,
    TimeProvider clock)
    : IDomainEventHandler<WeightEntryAddedDomainEvent>
{
    // v1 default — N7+ may pass per-user locale via DietReminderSettings or user profile.
    private const string DefaultLocale = "en";
    private const string GoalKind = "WeightTarget";

    public async Task HandleAsync(WeightEntryAddedDomainEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var goal = await userGoalRepository.GetByUserIdAsync(domainEvent.UserId, ct);
        if (goal is null) return;

        if (!goal.ShouldEmitWeightMilestone(domainEvent.WeightKg)) return;

        var now = clock.GetUtcNow().UtcDateTime;
        goal.MarkMilestoneAchieved(now);
        userGoalRepository.Update(goal);

        var integrationEvent = new GoalMilestoneReachedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredAt: now,
            UserId: domainEvent.UserId,
            Locale: DefaultLocale,
            GoalKind: GoalKind,
            MilestoneLabel: $"Reached target weight of {goal.TargetWeightKg} kg",
            Value: goal.TargetWeightKg);

        await integrationEventBus.PublishAsync(integrationEvent, ct);
    }
}
