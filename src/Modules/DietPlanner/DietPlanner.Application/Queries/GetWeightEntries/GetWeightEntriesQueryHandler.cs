namespace DietPlanner.Application.Queries.GetWeightEntries;

using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetWeightEntriesQueryHandler
    : IQueryHandler<GetWeightEntriesQuery, IReadOnlyList<WeightEntryDto>>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetWeightEntriesQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<IReadOnlyList<WeightEntryDto>> HandleAsync(
        GetWeightEntriesQuery query, CancellationToken ct = default)
        => await _dbContext.WeightEntries
            .AsNoTracking()
            .Where(e => e.PersonId == query.PersonId
                && (query.From == null || e.Date >= query.From)
                && (query.To == null || e.Date <= query.To))
            .OrderBy(e => e.Date)
            .Select(e => new WeightEntryDto(e.Id.Value, e.Date, e.WeightKg, e.CreatedAt))
            .ToListAsync(ct);
}
