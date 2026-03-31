namespace DietPlanner.Application.Commands.DeleteRecipe;

using Shared.Abstractions.CQRS;

public sealed record DeleteRecipeCommand(Guid Id, string UserId) : ICommand;
