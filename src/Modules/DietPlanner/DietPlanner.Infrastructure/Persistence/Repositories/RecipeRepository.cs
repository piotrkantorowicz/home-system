namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class RecipeRepository : IRecipeRepository
{
    private readonly DietPlannerDbContext _dbContext;

    public RecipeRepository(DietPlannerDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<Recipe?> GetByIdAsync(RecipeId id, CancellationToken ct = default)
        => await _dbContext.Recipes
            .Include(r => r.Ingredients)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<Recipe?> GetByNameAsync(string name, string userId, CancellationToken ct = default)
        => await _dbContext.Recipes
            .Include(r => r.Ingredients)
            .FirstOrDefaultAsync(x => x.Name == name && x.CreatedByUserId == userId, ct);

    public async Task<IReadOnlyList<Recipe>> GetByNamesAsync(
        IReadOnlyCollection<string> names, string userId, CancellationToken ct = default)
    {
        if (names.Count == 0) return [];
        var lowered = names.Select(n => n.ToLowerInvariant()).Distinct().ToList();
        return await _dbContext.Recipes
            .Include(r => r.Ingredients)
#pragma warning disable CA1304, CA1311, CA1862 // REASON: translated to SQL lower(); EF cannot translate ToLowerInvariant or a culture overload
            .Where(x => x.CreatedByUserId == userId && lowered.Contains(x.Name.ToLower()))
#pragma warning restore CA1304, CA1311, CA1862
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Recipe recipe, CancellationToken ct = default)
        => await _dbContext.Recipes.AddAsync(recipe, ct);

    public void Update(Recipe recipe)
    {
        // Recipe reads are tracked; marking the whole graph modified overwrites concurrent photo changes.
        if (_dbContext.Entry(recipe).State == EntityState.Detached)
            throw new InvalidOperationException("Recipe updates require a tracked recipe.");
        _dbContext.ChangeTracker.DetectChanges();
    }

    public void Delete(Recipe recipe)
        => _dbContext.Recipes.Remove(recipe);
}
