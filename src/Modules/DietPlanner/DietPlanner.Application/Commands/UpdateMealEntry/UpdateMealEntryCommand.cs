namespace DietPlanner.Application.Commands.UpdateMealEntry;

using Shared.Abstractions.Cqrs;

public sealed record UpdateMealEntryCommand(
    Guid Id,
    string UserId,
    DateOnly Date,
    Guid MealSlotId,
    Guid RecipeId,
    decimal Servings,
    string? Notes,
    TimeOnly? MealTime,
    int? SequenceOrder) : ICommand;
