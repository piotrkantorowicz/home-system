namespace DietPlanner.Application.Queries.GetRecipeById;

using DietPlanner.Application.Queries.SearchRecipes;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads one recipe with its ingredients and calculated nutrition; <see langword="null"/> when it does not exist or is soft-deleted.
/// </summary>
/// <param name="Id">Identifier of the recipe.</param>
/// <param name="UserId">Auth subject of the caller.</param>
public sealed record GetRecipeByIdQuery(Guid Id, string UserId) : IQuery<RecipeDto?>;
