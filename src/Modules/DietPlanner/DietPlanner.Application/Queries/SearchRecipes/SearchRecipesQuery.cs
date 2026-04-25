namespace DietPlanner.Application.Queries.SearchRecipes;

using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Core.Pagination;

public sealed record SearchRecipesQuery(
    string? Search,
    bool OnlyMine,
    string UserId,
    int Page,
    int PageSize) : IQuery<PagedList<RecipeDto>>;
