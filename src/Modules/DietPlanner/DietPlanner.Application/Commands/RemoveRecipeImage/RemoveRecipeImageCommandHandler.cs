namespace DietPlanner.Application.Commands.RemoveRecipeImage;

using DietPlanner.Application.Households;
using DietPlanner.Application.Images;
using Shared.Abstractions.Cqrs;

internal sealed class RemoveRecipeImageCommandHandler(IRecipeImagePersistence persistence,
    HouseholdRosterProvider households) : ICommandHandler<RemoveRecipeImageCommand>
{
    public Task HandleAsync(RemoveRecipeImageCommand command, CancellationToken ct)
        => persistence.RemoveAsync(command.Id, async (recipe, mutationCt) =>
        {
            LibraryAccess access = await households.GetLibraryAccessAsync(command.UserId, mutationCt);
            access.DemandEdit(recipe.CreatedByUserId, recipe.Visibility, "Recipe", command.Id);
            recipe.ChangeImage(null);
        }, ct);
}
