namespace DietPlanner.Application.Commands.DeleteRecipe;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class DeleteRecipeCommandHandler(
    IRecipeRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<DeleteRecipeCommand>
{
    public async Task HandleAsync(DeleteRecipeCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var recipe = await repository.GetByIdAsync(RecipeId.From(command.Id), ct)
            ?? throw new NotFoundException("Recipe", command.Id);

        if (recipe.CreatedByUserId != command.UserId)
            throw new DietPlannerDomainException("You can only delete recipes you created.");

        recipe.SoftDelete(now);
        repository.Update(recipe);
        await unitOfWork.CommitAsync(ct);
    }
}
