namespace DietPlanner.Domain.Ledgers;

using DietPlanner.Domain.ValueObjects;

/// <summary>Durable upload intent and deletion tombstone for immutable image objects.</summary>
public sealed class RecipeImageRevision
{
    private RecipeImageRevision() { }

    /// <summary>Revision key; persisted before any object is written.</summary>
    public Guid Id { get; private set; }
    /// <summary>Recipe that owns this revision.</summary>
    public RecipeId RecipeId { get; private set; } = default!;
    /// <summary>Earliest time an abandoned upload may be reclaimed.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }
    /// <summary>Whether the revision was successfully published.</summary>
    public bool Published { get; private set; }
    /// <summary>Irreversible retirement; retired revisions cannot be published.</summary>
    public bool Retired { get; private set; }
    /// <summary>Next deletion attempt; tombstones also catch delayed object writes after crashes.</summary>
    public DateTimeOffset NextCleanupAt { get; private set; }

    /// <summary>Creates an upload intent.</summary>
    /// <param name="id">Revision.</param>
    /// <param name="recipeId">Recipe.</param>
    /// <param name="now">Current time.</param>
    public static RecipeImageRevision Create(Guid id, RecipeId recipeId, DateTimeOffset now)
        => new() { Id = id, RecipeId = recipeId, ExpiresAt = now.AddMinutes(15), NextCleanupAt = now };

    /// <summary>Publishes while holding the upload row lock.</summary>
    public void Publish()
    {
        if (Retired || Published)
            throw new InvalidOperationException("Image revision is no longer pending.");
        Published = true;
    }

    /// <summary>Marks an unreferenced revision permanently unpublishable.</summary>
    public void Retire() => Retired = true;

    /// <summary>Schedules another idempotent deletion sweep.</summary>
    /// <param name="now">Current time.</param>
    public void Cleaned(DateTimeOffset now) => NextCleanupAt = now.AddDays(1);

    /// <summary>Backs off a failed deletion without blocking newer orphans.</summary>
    /// <param name="now">Current time.</param>
    public void RetryCleanup(DateTimeOffset now) => NextCleanupAt = now.AddMinutes(5);
}
