namespace DietPlanner.Application.Images;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Ledgers;

/// <summary>Independent durable upload intents and locked image mutations across database and storage.</summary>
public interface IRecipeImagePersistence
{
    /// <summary>Commits the intent outside the request's ambient transaction, before object writes.</summary>
    /// <param name="revision">Intent.</param>
    /// <param name="ct">Cancellation.</param>
    Task CreatePendingAsync(RecipeImageRevision revision, CancellationToken ct);

    /// <summary>Locks the pending revision across writes, then locks the recipe and atomically publishes.</summary>
    /// <param name="revision">Intent.</param>
    /// <param name="write">Bounded object writes.</param>
    /// <param name="mutate">Rechecks current access and mutates the locked recipe.</param>
    /// <param name="ct">Cancellation.</param>
    Task PublishAsync(RecipeImageRevision revision, Func<CancellationToken, Task> write,
        Func<Recipe, CancellationToken, Task> mutate, CancellationToken ct);

    /// <summary>Locks and commits removal outside the ambient request transaction.</summary>
    /// <param name="recipeId">Recipe.</param>
    /// <param name="mutate">Checks current access and removes the image.</param>
    /// <param name="ct">Cancellation.</param>
    Task RemoveAsync(Guid recipeId, Func<Recipe, CancellationToken, Task> mutate, CancellationToken ct);
}
