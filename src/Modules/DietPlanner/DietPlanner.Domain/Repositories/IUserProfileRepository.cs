namespace DietPlanner.Domain.Repositories;

using DietPlanner.Domain.Aggregates;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(UserProfile profile, CancellationToken ct = default);
    void Update(UserProfile profile);
}
