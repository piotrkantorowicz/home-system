namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

/// <summary>
/// Write-side access to <see cref="Recipe"/> aggregates, loaded with their ingredients. Queries bypass this and read the DbContext directly.
/// </summary>
public interface IRecipeRepository
{
    /// <summary>Loads a recipe by identifier for mutation.</summary>
    /// <param name="id">The identifier.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked recipe, or <see langword="null"/> when it does not exist.</returns>
    Task<Recipe?> GetByIdAsync(RecipeId id, CancellationToken ct = default);
    /// <summary>Finds a user's recipe by exact name, to reject duplicates on create.</summary>
    /// <param name="name">The name to match.</param>
    /// <param name="userId">Auth subject of the creating user.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The matching recipe, or <see langword="null"/>.</returns>
    Task<Recipe?> GetByNameAsync(string name, string userId, CancellationToken ct = default);
    /// <summary>Stages a new recipe; it is written when the unit of work commits.</summary>
    /// <param name="recipe">The recipe to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(Recipe recipe, CancellationToken ct = default);
    /// <summary>Marks a loaded recipe as modified; the write happens on commit.</summary>
    /// <param name="recipe">The tracked recipe.</param>
    void Update(Recipe recipe);
    /// <summary>Marks a loaded recipe for removal (hard delete); the write happens on commit.</summary>
    /// <param name="recipe">The tracked recipe.</param>
    void Delete(Recipe recipe);
}
