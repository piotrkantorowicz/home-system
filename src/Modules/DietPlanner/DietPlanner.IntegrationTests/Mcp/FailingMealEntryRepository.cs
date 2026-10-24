namespace DietPlanner.IntegrationTests.Mcp;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Wraps the real meal entry repository and throws on the <see cref="FailAt"/>-th staged entry of a
/// request, to prove an import that fails half way commits nothing.
/// </summary>
/// <param name="inner">The real repository.</param>
internal sealed class FailingMealEntryRepository(IMealEntryRepository inner) : IMealEntryRepository
{
    public const int FailAt = 100;

    private int _added;

    /// <summary>Replaces the registered repository with this wrapper.</summary>
    /// <param name="services">The host's services.</param>
    public static void Install(IServiceCollection services)
    {
        var original = services.Single(d => d.ServiceType == typeof(IMealEntryRepository));
        var implementation = original.ImplementationType!;
        services.Remove(original);
        services.Add(new ServiceDescriptor(implementation, implementation, original.Lifetime));
        services.AddScoped<IMealEntryRepository>(sp => new FailingMealEntryRepository((IMealEntryRepository)sp.GetRequiredService(implementation)));
    }

    public Task AddAsync(MealEntry entry, CancellationToken ct = default)
        => ++_added == FailAt ? throw new InvalidOperationException("Injected failure.") : inner.AddAsync(entry, ct);

    public Task<MealEntry?> GetByIdAsync(MealEntryId id, CancellationToken ct = default) => inner.GetByIdAsync(id, ct);

    public Task<List<MealEntry>> GetByPersonAndDateRangeAsync(Guid personId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default)
        => inner.GetByPersonAndDateRangeAsync(personId, fromDate, toDate, ct);

    public Task<IReadOnlyList<(DateOnly Date, MealSlotId MealSlotId)>> GetOccupiedSlotsAsync(Guid personId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
        => inner.GetOccupiedSlotsAsync(personId, fromDate, toDate, ct);

    public Task<bool> AnyForSlotAsync(MealSlotId mealSlotId, CancellationToken ct = default) => inner.AnyForSlotAsync(mealSlotId, ct);

    public void Update(MealEntry entry) => inner.Update(entry);

    public void Delete(MealEntry entry) => inner.Delete(entry);
}
