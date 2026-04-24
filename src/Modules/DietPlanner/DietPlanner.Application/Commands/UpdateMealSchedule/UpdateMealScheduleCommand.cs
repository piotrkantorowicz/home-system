namespace DietPlanner.Application.Commands.UpdateMealSchedule;

using Shared.Abstractions.CQRS;

public sealed record MealSlotInput(Guid? Id, string Name, string DefaultTime);
public sealed record UpdateMealScheduleCommand(string UserId, IReadOnlyList<MealSlotInput> Slots) : ICommand;
