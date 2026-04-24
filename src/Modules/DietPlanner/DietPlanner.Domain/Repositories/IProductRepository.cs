namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default);
    Task<Product?> GetByNameAsync(string name, string userId, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    void Update(Product product);
    void Delete(Product product);
}
