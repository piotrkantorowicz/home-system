namespace DietPlanner.Application.Commands.OverrideMealEntry;

using Shared.Abstractions.CQRS;

public sealed record ActualProductInput(Guid ProductId, decimal Amount, string Unit);

public sealed record OverrideMealEntryCommand(
    Guid Id,
    string UserId,
    Guid? ActualRecipeId,
    IReadOnlyList<ActualProductInput> ActualProducts) : ICommand;
