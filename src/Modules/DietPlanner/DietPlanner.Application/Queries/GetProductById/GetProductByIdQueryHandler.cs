namespace DietPlanner.Application.Queries.GetProductById;

using DietPlanner.Application.Households;
using DietPlanner.Application.Persistence;
using DietPlanner.Application.Queries.SearchProducts;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetProductByIdQueryHandler
    : IQueryHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IDietPlannerReadDbContext _dbContext;
    private readonly HouseholdRosterProvider _households;

    public GetProductByIdQueryHandler(IDietPlannerReadDbContext dbContext, HouseholdRosterProvider households)
    {
        _dbContext = dbContext;
        _households = households;
    }

    public async Task<ProductDto?> HandleAsync(
        GetProductByIdQuery query, CancellationToken ct = default)
    {
        LibraryAccess access = await _households.GetLibraryAccessAsync(query.UserId, ct);
        return await access.Visible(_dbContext.Products.AsNoTracking())
            .Where(p => p.Id == ProductId.From(query.Id))
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
                p.CreatedByUserId == query.UserId,
                p.Visibility.ToString(),
                access.CanEdit(p.CreatedByUserId, p.Visibility)))
            .FirstOrDefaultAsync(ct);
    }
}
