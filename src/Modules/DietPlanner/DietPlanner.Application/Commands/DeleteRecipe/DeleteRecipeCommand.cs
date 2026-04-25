namespace DietPlanner.Application.Commands.DeleteRecipe;

using Shared.Abstractions.Cqrs;

public sealed record DeleteRecipeCommand(Guid Id, string UserId) : ICommand;
