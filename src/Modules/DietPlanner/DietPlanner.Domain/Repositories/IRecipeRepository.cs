namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

public interface IRecipeRepository
{
    Task<Recipe?> GetByIdAsync(RecipeId id, CancellationToken ct = default);
    Task<Recipe?> GetByNameAsync(string name, string userId, CancellationToken ct = default);
    Task AddAsync(Recipe recipe, CancellationToken ct = default);
    void Update(Recipe recipe);
    void Delete(Recipe recipe);
}
