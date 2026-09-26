namespace DietPlanner.Application.Commands.DeleteRecipe;

using DietPlanner.Application.Households;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class DeleteRecipeCommandHandler(
    IRecipeRepository repository,
    HouseholdRosterProvider households,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<DeleteRecipeCommand>
{
    public async Task HandleAsync(DeleteRecipeCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var recipe = await repository.GetByIdAsync(RecipeId.From(command.Id), ct)
            ?? throw new NotFoundException("Recipe", command.Id);

        LibraryAccess access = await households.GetLibraryAccessAsync(command.UserId, ct);
        access.DemandEdit(recipe.CreatedByUserId, recipe.Visibility, "Recipe", command.Id);

        recipe.SoftDelete(now);
        repository.Update(recipe);
        await unitOfWork.CommitAsync(ct);
    }
}
