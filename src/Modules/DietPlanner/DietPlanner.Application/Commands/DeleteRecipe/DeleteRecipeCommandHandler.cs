namespace DietPlanner.Application.Commands.DeleteRecipe;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Core.Domain;

internal sealed class DeleteRecipeCommandHandler : ICommandHandler<DeleteRecipeCommand>
{
    private readonly IRecipeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRecipeCommandHandler(IRecipeRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(DeleteRecipeCommand command, CancellationToken ct = default)
    {
        var recipe = await _repository.GetByIdAsync(RecipeId.From(command.Id), ct)
            ?? throw new NotFoundException("Recipe", command.Id);

        if (recipe.CreatedByUserId != command.UserId)
            throw new DietPlannerDomainException("You can only delete recipes you created.");

        recipe.SoftDelete();
        _repository.Update(recipe);
        await _unitOfWork.CommitAsync(ct);
    }
}
