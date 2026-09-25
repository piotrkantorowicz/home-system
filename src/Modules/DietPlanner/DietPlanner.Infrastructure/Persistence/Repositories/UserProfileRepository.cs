namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly DietPlannerDbContext _dbContext;

    public UserProfileRepository(DietPlannerDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<UserProfile?> GetByPersonIdAsync(Guid personId, CancellationToken ct = default)
        => await _dbContext.UserProfiles.FirstOrDefaultAsync(x => x.PersonId == personId, ct);

    public async Task AddAsync(UserProfile profile, CancellationToken ct = default)
        => await _dbContext.UserProfiles.AddAsync(profile, ct);

    public void Update(UserProfile profile)
        => _dbContext.UserProfiles.Update(profile);
}
