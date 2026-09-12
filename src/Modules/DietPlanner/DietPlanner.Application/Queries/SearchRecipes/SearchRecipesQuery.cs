namespace DietPlanner.Application.Queries.SearchRecipes;

using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

public sealed record SearchRecipesQuery(
    string? Search,
    bool OnlyMine,
    string UserId,
    int Page,
    int PageSize) : IQuery<PagedList<RecipeDto>>;
