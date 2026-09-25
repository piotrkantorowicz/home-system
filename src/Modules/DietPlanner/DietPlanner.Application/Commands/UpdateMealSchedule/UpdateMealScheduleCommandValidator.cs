namespace DietPlanner.Application.Commands.UpdateMealSchedule;

using Shared.Abstractions.Cqrs;

internal sealed class UpdateMealScheduleCommandValidator : ICommandValidator<UpdateMealScheduleCommand>
{
    public IEnumerable<ValidationError> Validate(UpdateMealScheduleCommand command)
    {
        if (command.PersonId == Guid.Empty)
            yield return new ValidationError(nameof(command.PersonId), "PersonId is required.");

        if (command.Slots.Count < 1)
            yield return new ValidationError(nameof(command.Slots), "At least 1 meal slot is required.");

        if (command.Slots.Count > 8)
            yield return new ValidationError(nameof(command.Slots), "Cannot have more than 8 meal slots.");

        for (var i = 0; i < command.Slots.Count; i++)
        {
            MealSlotInput slot = command.Slots[i];

            if (string.IsNullOrWhiteSpace(slot.Name))
                yield return new ValidationError($"Slots[{i}].Name", "Slot name is required.");

            if (!TimeOnly.TryParse(slot.DefaultTime, out _))
                yield return new ValidationError($"Slots[{i}].DefaultTime", "DefaultTime must be a valid time in HH:mm format.");
        }
    }
}
