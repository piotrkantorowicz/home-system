namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

/// <summary>
/// Write-side access to <see cref="Product"/> aggregates. Reads here exclude soft-deleted products unless stated. Queries bypass this and read the DbContext directly.
/// </summary>
public interface IProductRepository
{
    /// <summary>Loads a product by identifier for mutation.</summary>
    /// <param name="id">The identifier.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked product, or <see langword="null"/> when it does not exist.</returns>
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken ct = default);
    /// <summary>Loads several products at once, e.g. every ingredient of a recipe.</summary>
    /// <param name="ids">The identifiers to load; unknown ids are skipped.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The products found, in no particular order.</returns>
    Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<ProductId> ids, CancellationToken ct = default);
    /// <summary>Finds a user's product by exact name, to reject duplicates on create.</summary>
    /// <param name="name">The name to match.</param>
    /// <param name="userId">Auth subject of the creating user.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The matching product, or <see langword="null"/>.</returns>
    Task<Product?> GetByNameAsync(string name, string userId, CancellationToken ct = default);
    /// <summary>Stages a new product; it is written when the unit of work commits.</summary>
    /// <param name="product">The product to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(Product product, CancellationToken ct = default);
    /// <summary>Marks a loaded product as modified; the write happens on commit.</summary>
    /// <param name="product">The tracked product.</param>
    void Update(Product product);
    /// <summary>Marks a loaded product for removal (hard delete); the write happens on commit.</summary>
    /// <param name="product">The tracked product.</param>
    void Delete(Product product);
}
