namespace DietPlanner.Application.Commands.UpdateMealSchedule;

using Shared.Abstractions.CQRS;

public sealed record MealSlotInput(string Name, string DefaultTime);
public sealed record UpdateMealScheduleCommand(string UserId, IReadOnlyList<MealSlotInput> Slots) : ICommand;
