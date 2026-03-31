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

    public async Task<Product?> GetByNameAsync(string name, string userId, CancellationToken ct = default)
        => await _dbContext.Products
            .FirstOrDefaultAsync(x => x.Name == name && x.CreatedByUserId == userId, ct);

    public async Task AddAsync(Product product, CancellationToken ct = default)
        => await _dbContext.Products.AddAsync(product, ct);

    public void Update(Product product)
        => _dbContext.Products.Update(product);

    public void Delete(Product product)
        => _dbContext.Products.Remove(product);
}
