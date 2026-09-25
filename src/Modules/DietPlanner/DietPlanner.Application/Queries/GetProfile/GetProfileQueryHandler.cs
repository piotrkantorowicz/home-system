namespace DietPlanner.Application.Queries.GetProfile;

using DietPlanner.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetProfileQueryHandler : IQueryHandler<GetProfileQuery, UserProfileDto?>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetProfileQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<UserProfileDto?> HandleAsync(GetProfileQuery query, CancellationToken ct = default)
        => await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.PersonId == query.PersonId)
            .Select(p => new UserProfileDto(
                p.Id.Value,
                p.PersonId,
                p.DateOfBirth,
                p.Gender.HasValue ? p.Gender.Value.ToString() : null,
                p.HeightCm,
                p.CurrentWeightKg,
                p.TargetWeightKg,
                p.ActivityLevel.HasValue ? p.ActivityLevel.Value.ToString() : null,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(ct);
}
