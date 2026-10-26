namespace DietPlanner.Application.Commands.UploadRecipeImage;

using System.Transactions;
using DietPlanner.Application.Households;
using DietPlanner.Application.Images;
using DietPlanner.Application.Persistence;
using DietPlanner.Application.Storage;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class UploadRecipeImageCommandHandler(
    IDietPlannerReadDbContext db, HouseholdRosterProvider households,
    IRecipeImageProcessor processor, IRecipeImagePersistence persistence,
    IImageObjectStorage storage, TimeProvider clock) : ICommandHandler<UploadRecipeImageCommand, RecipeImageDto>
{
    public async Task<RecipeImageDto> HandleAsync(UploadRecipeImageCommand command, CancellationToken ct = default)
    {
        // Persistence owns independent transactions; do not retain an enlisted read connection during S3 writes.
        using var suppression = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
        LibraryAccess access = await households.GetLibraryAccessAsync(command.UserId, ct);
        var header = await db.Recipes.AsNoTracking().Where(r => r.Id == RecipeId.From(command.Id))
            .Select(r => new { r.CreatedByUserId, r.Visibility, Version = r.Image == null ? (Guid?)null : r.Image.Version })
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Recipe", command.Id);
        access.DemandEdit(header.CreatedByUserId, header.Visibility, "Recipe", command.Id);

        ProcessedRecipeImage processed = await processor.ProcessAsync(command.Content, command.ContentType, command.FileName, ct);
        RecipeImageDto image = processed.Metadata;
        var revision = RecipeImageRevision.Create(image.Version, RecipeId.From(command.Id), clock.GetUtcNow());
        await persistence.CreatePendingAsync(revision, ct);
        await persistence.PublishAsync(revision, async writeCt =>
        {
            using var detail = new MemoryStream(processed.Detail, false);
            using var thumbnail = new MemoryStream(processed.Thumbnail, false);
            await storage.PutAsync(RecipeImageKeys.Detail(image.Version), detail, image.ContentType, writeCt);
            await storage.PutAsync(RecipeImageKeys.Thumbnail(image.Version), thumbnail, image.ContentType, writeCt);
        }, async (recipe, mutationCt) =>
        {
            LibraryAccess current = await households.GetLibraryAccessAsync(command.UserId, mutationCt);
            current.DemandEdit(recipe.CreatedByUserId, recipe.Visibility, "Recipe", command.Id);
            if (recipe.Image?.Version != header.Version)
                throw new ConflictException("Recipe photo changed during upload. Reload and retry.");
            recipe.ChangeImage(new RecipeImage(image.Version, image.Width, image.Height, image.Size,
                image.ThumbnailWidth, image.ThumbnailHeight, image.ThumbnailSize));
        }, ct);
        return image;
    }
}
