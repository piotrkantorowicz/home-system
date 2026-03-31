namespace DietPlanner.Application.Queries.SearchRecipes;

using Shared.Abstractions.CQRS;
using Shared.Abstractions.Pagination;

public sealed record SearchRecipesQuery(
    string? Search,
    bool OnlyMine,
    string UserId,
    int Page,
    int PageSize) : IQuery<PagedList<RecipeDto>>;
