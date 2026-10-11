namespace DietPlanner.Application.Queries.GetRecipeImage;

using DietPlanner.Application.Households;
using DietPlanner.Application.Images;
using DietPlanner.Application.Persistence;
using DietPlanner.Application.Storage;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetRecipeImageQueryHandler(IDietPlannerReadDbContext db,
    HouseholdRosterProvider households, IImageObjectStorage storage) : IQueryHandler<GetRecipeImageQuery, ImageObjectRead?>
{
    public async Task<ImageObjectRead?> HandleAsync(GetRecipeImageQuery query, CancellationToken ct)
    {
        LibraryAccess access = await households.GetLibraryAccessAsync(query.UserId, ct);
        Guid? version = await access.Visible(db.Recipes.AsNoTracking())
            .Where(r => r.Id == RecipeId.From(query.Id))
            .Select(r => r.Image == null ? (Guid?)null : r.Image.Version).SingleOrDefaultAsync(ct);
        if (version is null)
            return null;
        return await storage.OpenReadAsync(query.Thumbnail
            ? RecipeImageKeys.Thumbnail(version.Value) : RecipeImageKeys.Detail(version.Value), ct);
    }
}
