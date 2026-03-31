namespace DietPlanner.Application.Queries.GetRecipeById;

using DietPlanner.Application.Queries.SearchRecipes;
using Shared.Abstractions.CQRS;

public sealed record GetRecipeByIdQuery(Guid Id, string UserId) : IQuery<RecipeDto?>;
