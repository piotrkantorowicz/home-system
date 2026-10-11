namespace DietPlanner.Infrastructure.Images;

using System.Transactions;
using DietPlanner.Application.Images;
using DietPlanner.Application.Storage;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.ValueObjects;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Core.Domain;

internal sealed class RecipeImagePersistence(IServiceScopeFactory scopes) : IRecipeImagePersistence
{
    public async Task CreatePendingAsync(RecipeImageRevision revision, CancellationToken ct)
    {
        // The intent must survive failure of the command's ambient transaction.
        using var suppression = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        db.RecipeImageRevisions.Add(revision);
        await db.SaveChangesAsync(ct);
    }

    public async Task PublishAsync(RecipeImageRevision revision, Func<CancellationToken, Task> write,
        Func<Recipe, CancellationToken, Task> mutate, CancellationToken ct)
    {
        using var suppression = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        RecipeImageRevision pending = await db.RecipeImageRevisions.FromSqlInterpolated(
            $"SELECT * FROM recipe_image_revisions WHERE id = {revision.Id} FOR UPDATE").SingleAsync(ct);
        if (pending.Retired || pending.Published)
            throw new ConflictException("Photo upload expired. Retry the upload.");

        // Cleanup locks this same row with SKIP LOCKED, so no live write can be reclaimed.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(60));
        try
        {
            await write(deadline.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ImageStorageUnavailableException();
        }
        Recipe recipe = await LockRecipeAsync(db, revision.RecipeId.Value, ct);
        await mutate(recipe, ct);
        pending.Publish();
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task RemoveAsync(Guid recipeId, Func<Recipe, CancellationToken, Task> mutate, CancellationToken ct)
    {
        using var suppression = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        Recipe recipe = await LockRecipeAsync(db, recipeId, ct);
        await mutate(recipe, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private static async Task<Recipe> LockRecipeAsync(DietPlannerDbContext db, Guid id, CancellationToken ct)
    {
        // Execute separately: EF's owned-image join must not widen the FOR UPDATE target.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM recipes WHERE id = {id} FOR UPDATE", ct);
        return await db.Recipes.SingleOrDefaultAsync(r => r.Id == RecipeId.From(id), ct)
            ?? throw new NotFoundException("Recipe", id);
    }
}
