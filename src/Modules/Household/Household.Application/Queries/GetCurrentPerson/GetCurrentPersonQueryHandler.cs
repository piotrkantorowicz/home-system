namespace Household.Application.Queries.GetCurrentPerson;

using Household.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetCurrentPersonQueryHandler
    : IQueryHandler<GetCurrentPersonQuery, CurrentPersonDto?>
{
    private readonly IHouseholdReadDbContext _dbContext;

    public GetCurrentPersonQueryHandler(IHouseholdReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<CurrentPersonDto?> HandleAsync(GetCurrentPersonQuery query, CancellationToken ct)
        => await _dbContext.Persons
            .AsNoTracking()
            .Where(p => p.AuthSubject == query.AuthSubject)
            .Select(p => new CurrentPersonDto(
                p.Id.Value,
                p.DisplayName,
                p.Email == null ? null : p.Email.Value,
                p.AvatarUrl,
                p.IsManaged))
            .FirstOrDefaultAsync(ct);
}
