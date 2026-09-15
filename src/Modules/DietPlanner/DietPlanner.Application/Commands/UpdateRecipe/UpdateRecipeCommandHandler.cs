namespace DietPlanner.Application.Commands.UpdateRecipe;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateRecipeCommandHandler(
    IRecipeRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<UpdateRecipeCommand>
{
    public async Task HandleAsync(UpdateRecipeCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var recipe = await repository.GetByIdAsync(RecipeId.From(command.Id), ct)
            ?? throw new NotFoundException("Recipe", command.Id);

        if (recipe.CreatedByUserId != command.UserId)
            throw new DietPlannerDomainException("You can only update recipes you created.");

        recipe.Update(command.Name, command.Description, command.Instructions,
            command.Servings, command.PrepTimeMinutes, now);

        recipe.ClearIngredients();
        foreach (var ing in command.Ingredients)
            recipe.AddIngredient(RecipeIngredientId.New(), ProductId.From(ing.ProductId), ing.Amount, ing.Unit);

        repository.Update(recipe);
        await unitOfWork.CommitAsync(ct);
    }
}
