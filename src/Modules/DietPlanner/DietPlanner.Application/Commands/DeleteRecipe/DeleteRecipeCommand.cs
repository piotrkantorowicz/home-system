namespace DietPlanner.Application.Commands.DeleteRecipe;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Soft-deletes a recipe so it disappears from lists while past meals keep resolving it.
/// </summary>
/// <param name="Id">Identifier of the recipe; must be owned by the caller.</param>
/// <param name="UserId">Auth subject of the caller; the command only touches this user's data.</param>
public sealed record DeleteRecipeCommand(Guid Id, string UserId) : ICommand;
