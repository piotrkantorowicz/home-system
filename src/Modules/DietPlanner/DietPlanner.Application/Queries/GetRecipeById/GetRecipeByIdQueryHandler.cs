namespace DietPlanner.Application.Queries.GetRecipeById;

using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Application.Queries.SearchRecipes;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.CQRS;

internal sealed class GetRecipeByIdQueryHandler
    : IQueryHandler<GetRecipeByIdQuery, RecipeDto?>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetRecipeByIdQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<RecipeDto?> HandleAsync(
        GetRecipeByIdQuery query, CancellationToken ct = default)
        => await _dbContext.Recipes
            .AsNoTracking()
            .Include(Recipe.IngredientsField)
            .Where(r => r.Id.Value == query.Id)
            .Select(r => new RecipeDto(
                r.Id.Value,
                r.Name,
                r.Description,
                r.Instructions,
                r.Servings,
                r.PrepTimeMinutes,
                r.CreatedByUserId,
                r.CreatedAt,
                r.UpdatedAt,
                r.CreatedByUserId == query.UserId,
                r.Ingredients.Select(i => new RecipeIngredientDto(
                    i.Id.Value,
                    i.ProductId.Value,
                    i.Amount,
                    i.Unit)).ToList()))
            .FirstOrDefaultAsync(ct);
}
