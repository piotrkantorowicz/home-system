namespace DietPlanner.Application.Commands.UpdateMealSchedule;

using System.Globalization;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateMealScheduleCommandHandler(
    IMealScheduleConfigRepository repository,
    IMealEntryRepository mealEntryRepository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<UpdateMealScheduleCommand>
{
    public async Task HandleAsync(UpdateMealScheduleCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        MealScheduleConfig? config = await repository.GetByPersonIdAsync(command.PersonId, ct);

        if (config is null)
        {
            // First-time creation — all inputs must be brand new (no ids yet).
            var newSlots = command.Slots
                .Select(s => (s.Name, TimeOnly.Parse(s.DefaultTime, CultureInfo.InvariantCulture)))
                .ToList();

            config = MealScheduleConfig.Create(MealScheduleConfigId.New(), command.PersonId, newSlots, now);
            await repository.AddAsync(config, ct);
        }
        else
        {
            var upserts = command.Slots
                .Select(s => new MealSlotUpsert(
                    s.Id is null ? null : MealSlotId.From(s.Id.Value),
                    s.Name,
                    TimeOnly.Parse(s.DefaultTime, CultureInfo.InvariantCulture)))
                .ToList();

            // Block deletion of slots that have entries.
            var removedIds = config.ComputeRemovedSlots(upserts);
            foreach (var slotId in removedIds)
            {
                if (await mealEntryRepository.AnyForSlotAsync(slotId, ct))
                    throw new DietPlannerDomainException(
                        "Cannot delete a meal slot that has logged entries. Remove or reassign the entries first.");
            }

            config.ApplyUpdate(upserts, now);
            repository.Update(config);
        }

        await unitOfWork.CommitAsync(ct);
    }
}
