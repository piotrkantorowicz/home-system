namespace DietPlanner.Application.Households;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// The caller's access to the recipe / product library (#230). The library is still keyed by the
/// creator's auth subject, so the household is resolved to its members' subjects.
/// Read: the creator always, <see cref="Visibility.Household"/> for the creator's household,
/// <see cref="Visibility.Public"/> for everyone. Write (create / edit / delete): never a Guest
/// (design §3); otherwise the creator, plus an Owner/Adult of the creator's household for a
/// non-private item.
/// </summary>
/// <param name="CallerSubject">Auth subject of the caller.</param>
/// <param name="CallerRole">The caller's household role, or <see langword="null"/> without a household.</param>
/// <param name="HouseholdSubjects">Auth subjects of the caller's household members; empty without a household.</param>
internal sealed record LibraryAccess(
    string CallerSubject,
    string? CallerRole,
    IReadOnlyList<string> HouseholdSubjects)
{
    private bool CallerIsAdult => CallerRole is "Owner" or "Adult";

    /// <summary>Whether the caller may add to the library at all; a Guest only reads.</summary>
    public bool CanWrite => CallerRole != "Guest";

    /// <summary>Whether the caller can see an item by <paramref name="createdBy"/>.</summary>
    public bool CanRead(string createdBy, Visibility visibility)
        => createdBy == CallerSubject
            || visibility == Visibility.Public
            || (visibility == Visibility.Household && HouseholdSubjects.Contains(createdBy));

    /// <summary>Whether the caller can edit or delete an item by <paramref name="createdBy"/>.</summary>
    public bool CanEdit(string createdBy, Visibility visibility)
        => CanWrite
            && (createdBy == CallerSubject
                || (visibility != Visibility.Private && CallerIsAdult && HouseholdSubjects.Contains(createdBy)));

    /// <summary>Throws 403 for a Guest.</summary>
    public void DemandWrite()
    {
        if (!CanWrite)
            throw new ForbiddenException("Guests cannot change the recipe and product library.");
    }

    /// <summary>Throws 404 when <paramref name="recipe"/> is missing or not visible to the caller.</summary>
    public void DemandReadable(Recipe? recipe, Guid recipeId)
    {
        if (recipe is null || !CanRead(recipe.CreatedByUserId, recipe.Visibility))
            throw new NotFoundException("Recipe", recipeId);
    }

    /// <summary>Throws 404 for the first of <paramref name="requested"/> missing from <paramref name="found"/> or not visible.</summary>
    public void DemandReadable(IEnumerable<Product> found, IEnumerable<ProductId> requested)
    {
        var readable = found.Where(p => CanRead(p.CreatedByUserId, p.Visibility)).Select(p => p.Id).ToHashSet();
        foreach (var id in requested)
        {
            if (!readable.Contains(id))
                throw new NotFoundException("Product", id.Value);
        }
    }

    /// <summary>Throws 404 for an item the caller cannot see, 403 for one they see but may not edit.</summary>
    public void DemandEdit(string createdBy, Visibility visibility, string resource, Guid resourceId)
    {
        if (!CanRead(createdBy, visibility))
            throw new NotFoundException(resource, resourceId);
        if (!CanEdit(createdBy, visibility))
            throw new ForbiddenException($"You can only change this {resource} if you created it or are an adult of its household.");
    }

    /// <summary>Filters <paramref name="recipes"/> to those <see cref="CanRead"/> allows; translates to SQL.</summary>
    public IQueryable<Recipe> Visible(IQueryable<Recipe> recipes)
    {
        string caller = CallerSubject;
        IReadOnlyList<string> household = HouseholdSubjects;
        return recipes.Where(r => r.CreatedByUserId == caller
            || r.Visibility == Visibility.Public
            || (r.Visibility == Visibility.Household && household.Contains(r.CreatedByUserId)));
    }

    /// <summary>Filters <paramref name="products"/> to those <see cref="CanRead"/> allows; translates to SQL.</summary>
    public IQueryable<Product> Visible(IQueryable<Product> products)
    {
        string caller = CallerSubject;
        IReadOnlyList<string> household = HouseholdSubjects;
        return products.Where(p => p.CreatedByUserId == caller
            || p.Visibility == Visibility.Public
            || (p.Visibility == Visibility.Household && household.Contains(p.CreatedByUserId)));
    }
}
