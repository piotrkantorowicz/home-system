namespace DietPlanner.Application.Commands.CreateMealEntry;

using Shared.Abstractions.CQRS;

public sealed record CreateMealEntryCommand(
    string UserId,
    DateOnly Date,
    Guid MealSlotId,
    Guid RecipeId,
    decimal Servings,
    string? Notes,
    TimeOnly? MealTime,
    int? SequenceOrder) : ICommand<Guid>;
