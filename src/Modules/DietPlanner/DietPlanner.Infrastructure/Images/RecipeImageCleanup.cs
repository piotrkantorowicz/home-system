namespace DietPlanner.Infrastructure.Images;

using DietPlanner.Application.Images;
using DietPlanner.Application.Storage;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class RecipeImageCleanup(DietPlannerDbContext db, IImageObjectStorage storage, TimeProvider clock)
{
    internal async Task RunOnceAsync(CancellationToken ct)
    {
        DateTimeOffset now = clock.GetUtcNow();
        Guid[] candidates = await db.RecipeImageRevisions.AsNoTracking()
            .Where(r => r.NextCleanupAt <= now && (r.Published || r.ExpiresAt <= now)
                && !db.Recipes.Any(recipe => recipe.Image != null && recipe.Image.Version == r.Id))
            .OrderBy(r => r.NextCleanupAt).ThenBy(r => r.Id).Select(r => r.Id).Take(50).ToArrayAsync(ct);
        foreach (Guid id in candidates)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            RecipeImageRevision? revision = await db.RecipeImageRevisions.FromSqlInterpolated(
                $"SELECT * FROM recipe_image_revisions WHERE id = {id} FOR UPDATE SKIP LOCKED").SingleOrDefaultAsync(ct);
            if (revision is null || await db.Recipes.AnyAsync(r => r.Image != null && r.Image.Version == id, ct))
                continue;
            revision.Retire();
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            // Retirement is committed before deleting objects; a crash here remains retryable.
            await storage.DeleteAsync(RecipeImageKeys.Detail(id), ct);
            await storage.DeleteAsync(RecipeImageKeys.Thumbnail(id), ct);
            revision.Cleaned(now);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
    }
}
