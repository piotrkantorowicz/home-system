using DietPlanner.Api.Common.Exceptions;
using DietPlanner.Api.Common.Models;
using DietPlanner.Api.Data;
using DietPlanner.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DietPlanner.Api.Features.Products;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> SearchAsync(
        string? search,
        bool onlyMine,
        string userId,
        int page,
        int pageSize);

    Task<ProductResponse> GetByIdAsync(Guid id, string userId);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, string userId);
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, string userId);
    Task DeleteAsync(Guid id, string userId, bool permanent = false);
}

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductService> _logger;
    private readonly IWebHostEnvironment _env;

    public ProductService(
        AppDbContext db,
        ILogger<ProductService> logger,
        IWebHostEnvironment env)
    {
        _db = db;
        _logger = logger;
        _env = env;
    }

    public async Task<PagedResult<ProductResponse>> SearchAsync(
        string? search,
        bool onlyMine,
        string userId,
        int page,
        int pageSize)
    {
        var query = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));
        }

        if (onlyMine)
        {
            query = query.Where(p => p.CreatedByUserId == userId);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ProductResponse>
        {
            Items = items.Select(p => ProductResponse.FromEntity(p, userId)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id, string userId)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        return ProductResponse.FromEntity(product, userId);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, string userId)
    {
        var existingProduct = await _db.Products
            .FirstOrDefaultAsync(p => p.Name.ToLower() == request.Name.ToLower());

        if (existingProduct != null)
        {
            throw new ValidationException($"A product with the name '{request.Name}' already exists");
        }

        var product = new Product
        {
            Name = request.Name,
            CaloriesPer100g = request.CaloriesPer100g,
            ProteinPer100g = request.ProteinPer100g,
            CarbsPer100g = request.CarbsPer100g,
            FatPer100g = request.FatPer100g,
            FiberPer100g = request.FiberPer100g,
            DefaultUnit = request.DefaultUnit,
            DensityGramsPerMl = request.DensityGramsPerMl,
            GramPerPiece = request.GramPerPiece,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Product created: {ProductId} by user {UserId}", product.Id, userId);

        return ProductResponse.FromEntity(product, userId);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, string userId)
    {
        var product = await _db.Products.FindAsync(id);

        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        if (product.CreatedByUserId != userId)
        {
            throw new ForbiddenException("You can only update products you created");
        }

        if (product.Name.ToLower() != request.Name.ToLower())
        {
            var nameExists = await _db.Products
                .AnyAsync(p => p.Id != id && p.Name.ToLower() == request.Name.ToLower());

            if (nameExists)
            {
                throw new ValidationException($"A product with the name '{request.Name}' already exists");
            }
        }

        product.Name = request.Name;
        product.CaloriesPer100g = request.CaloriesPer100g;
        product.ProteinPer100g = request.ProteinPer100g;
        product.CarbsPer100g = request.CarbsPer100g;
        product.FatPer100g = request.FatPer100g;
        product.FiberPer100g = request.FiberPer100g;
        product.DefaultUnit = request.DefaultUnit;
        product.DensityGramsPerMl = request.DensityGramsPerMl;
        product.GramPerPiece = request.GramPerPiece;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Product updated: {ProductId} by user {UserId}", product.Id, userId);

        return ProductResponse.FromEntity(product, userId);
    }

    public async Task DeleteAsync(Guid id, string userId, bool permanent = false)
    {
        // Use IgnoreQueryFilters to find soft-deleted products for permanent deletion
        var product = await _db.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        if (!permanent && product.DeletedAt != null)
        {
            throw new NotFoundException("Product", id);
        }

        if (product.CreatedByUserId != userId)
        {
            throw new ForbiddenException("You can only delete products you created");
        }

        if (permanent)
        {
            if (_env.IsProduction())
            {
                throw new ForbiddenException("Permanent deletion is not allowed in production environment");
            }

            var isUsedInRecipes = await _db.RecipeIngredients
                .AnyAsync(ri => ri.ProductId == id);

            if (isUsedInRecipes)
            {
                throw new ValidationException(
                    "Cannot permanently delete product that is used in recipes. " +
                    "Remove it from recipes first or use soft delete.");
            }

            _db.Products.Remove(product);
            _logger.LogInformation("Product permanently deleted: {ProductId} by user {UserId}", product.Id, userId);
        }
        else
        {
            product.DeletedAt = DateTime.UtcNow;
            _logger.LogInformation("Product soft-deleted: {ProductId} by user {UserId}", product.Id, userId);
        }

        await _db.SaveChangesAsync();
    }
}
