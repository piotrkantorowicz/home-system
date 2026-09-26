namespace DietPlanner.Application.Commands.UpdateRecipe;

using DietPlanner.Application.Households;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateRecipeCommandHandler(
    IRecipeRepository repository,
    IProductRepository productRepository,
    HouseholdRosterProvider households,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<UpdateRecipeCommand>
{
    public async Task HandleAsync(UpdateRecipeCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var recipe = await repository.GetByIdAsync(RecipeId.From(command.Id), ct)
            ?? throw new NotFoundException("Recipe", command.Id);

        LibraryAccess access = await households.GetLibraryAccessAsync(command.UserId, ct);
        access.DemandEdit(recipe.CreatedByUserId, recipe.Visibility, "Recipe", command.Id);

        // Only newly added products must be visible; kept lines stay valid even if their product went private.
        var addedProductIds = command.Ingredients.Select(i => ProductId.From(i.ProductId))
            .Except(recipe.Ingredients.Select(i => i.ProductId))
            .ToList();
        if (addedProductIds.Count > 0)
            access.DemandReadable(await productRepository.GetByIdsAsync(addedProductIds, ct), addedProductIds);

        if (VisibilityInput.Parse(command.Visibility) is { } visibility && visibility != recipe.Visibility)
        {
            if (recipe.CreatedByUserId != command.UserId)
                throw new ForbiddenException("Only the creator can change who sees this recipe.");
            recipe.ChangeVisibility(visibility);
        }

        recipe.Update(command.Name, command.Description, command.Instructions,
            command.Servings, command.PrepTimeMinutes, now);

        recipe.ClearIngredients();
        foreach (var ing in command.Ingredients)
            recipe.AddIngredient(RecipeIngredientId.New(), ProductId.From(ing.ProductId), ing.Amount, ing.Unit);

        repository.Update(recipe);
        await unitOfWork.CommitAsync(ct);
    }
}
