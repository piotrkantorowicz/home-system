namespace DietPlanner.Application.Queries.GetProductById;

using DietPlanner.Application.Persistence;
using DietPlanner.Application.Queries.SearchProducts;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.CQRS;

internal sealed class GetProductByIdQueryHandler
    : IQueryHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetProductByIdQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<ProductDto?> HandleAsync(
        GetProductByIdQuery query, CancellationToken ct = default)
        => await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id.Value == query.Id)
            .Select(p => new ProductDto(
                p.Id.Value,
                p.Name,
                p.Nutrition.Calories,
                p.Nutrition.Protein,
                p.Nutrition.Carbs,
                p.Nutrition.Fat,
                p.Nutrition.Fiber,
                p.DefaultUnit,
                p.DensityGramsPerMl,
                p.GramPerPiece,
                p.CreatedByUserId,
                p.CreatedAt,
                p.UpdatedAt,
                p.CreatedByUserId == query.UserId))
            .FirstOrDefaultAsync(ct);
}
