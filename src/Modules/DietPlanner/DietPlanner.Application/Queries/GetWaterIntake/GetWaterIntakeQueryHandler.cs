namespace DietPlanner.Application.Queries.GetWaterIntake;

using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.CQRS;

internal sealed class GetWaterIntakeQueryHandler : IQueryHandler<GetWaterIntakeQuery, WaterIntakeListDto>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetWaterIntakeQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<WaterIntakeListDto> HandleAsync(GetWaterIntakeQuery query, CancellationToken ct = default)
    {
        var entries = await _dbContext.WaterIntakes
            .AsNoTracking()
            .Where(w => w.UserId == query.UserId && w.Date == query.Date)
            .OrderByDescending(w => w.Timestamp)
            .Select(w => new WaterIntakeEntryDto(
                w.Id.Value,
                w.AmountMl,
                w.Timestamp,
                w.Note))
            .ToListAsync(ct);

        var totalMl = entries.Sum(e => e.AmountMl);

        return new WaterIntakeListDto(query.Date, totalMl, entries);
    }
}
