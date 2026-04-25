namespace DietPlanner.Application.Queries.GetRecipeById;

using DietPlanner.Application.Queries.SearchRecipes;
using Shared.Abstractions.Cqrs;

public sealed record GetRecipeByIdQuery(Guid Id, string UserId) : IQuery<RecipeDto?>;
