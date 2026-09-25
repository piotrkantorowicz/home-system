namespace DietPlanner.Application.Queries.GetHydrationConfig;

using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetHydrationConfigQueryHandler : IQueryHandler<GetHydrationConfigQuery, HydrationConfigDto?>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetHydrationConfigQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<HydrationConfigDto?> HandleAsync(GetHydrationConfigQuery query, CancellationToken ct = default)
        => await _dbContext.HydrationConfigs
            .AsNoTracking()
            .Where(c => c.PersonId == query.PersonId)
            .Select(c => new HydrationConfigDto(
                c.Id.Value,
                c.PersonId,
                c.DailyWaterTargetMl,
                c.GlassSizeMl,
                c.TrackWaterIntake,
                c.CreatedAt,
                c.UpdatedAt))
            .FirstOrDefaultAsync(ct);
}
