namespace DietPlanner.IntegrationTests.Images;

using DietPlanner.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

internal sealed class PhotoCommitFailureInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context?.ChangeTracker.Entries<Recipe>().Any(e => e.Entity.Image is not null) == true)
            throw new InvalidOperationException("Injected photo commit failure.");
        return ValueTask.FromResult(result);
    }
}
