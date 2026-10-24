namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class ProductRepository : IProductRepository
{
    private readonly DietPlannerDbContext _dbContext;

    public ProductRepository(DietPlannerDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default)
        => await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IReadOnlyCollection<ProductId> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return [];
        return await _dbContext.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);
    }

    public async Task<Product?> GetByNameAsync(string name, string userId, CancellationToken ct = default)
        => await _dbContext.Products
            .FirstOrDefaultAsync(x => x.Name == name && x.CreatedByUserId == userId, ct);

    public async Task<IReadOnlyList<Product>> GetByNamesAsync(
        IReadOnlyCollection<string> names, string userId, CancellationToken ct = default)
    {
        if (names.Count == 0) return [];
        var lowered = names.Select(n => n.ToLowerInvariant()).Distinct().ToList();
        return await _dbContext.Products
#pragma warning disable CA1304, CA1311, CA1862 // REASON: translated to SQL lower(); EF cannot translate ToLowerInvariant or a culture overload
            .Where(x => x.CreatedByUserId == userId && lowered.Contains(x.Name.ToLower()))
#pragma warning restore CA1304, CA1311, CA1862
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
        => await _dbContext.Products.AddAsync(product, ct);

    public void Update(Product product)
        => _dbContext.Products.Update(product);

    public void Delete(Product product)
        => _dbContext.Products.Remove(product);
}
