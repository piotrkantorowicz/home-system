namespace DietPlanner.Application.Commands.UpdateMealSchedule;

using Shared.Abstractions.Cqrs;

public sealed record MealSlotInput(Guid? Id, string Name, string DefaultTime);
public sealed record UpdateMealScheduleCommand(string UserId, IReadOnlyList<MealSlotInput> Slots) : ICommand;
