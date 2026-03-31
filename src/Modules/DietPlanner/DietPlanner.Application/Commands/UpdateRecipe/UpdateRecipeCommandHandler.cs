namespace DietPlanner.Application.Commands.UpdateRecipe;

using DietPlanner.Application.Commands.CreateRecipe;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Domain;

internal sealed class UpdateRecipeCommandHandler : ICommandHandler<UpdateRecipeCommand>
{
    private readonly IRecipeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRecipeCommandHandler(IRecipeRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(UpdateRecipeCommand command, CancellationToken ct = default)
    {
        var recipe = await _repository.GetByIdAsync(RecipeId.From(command.Id), ct)
            ?? throw new NotFoundException("Recipe", command.Id);

        if (recipe.CreatedByUserId != command.UserId)
            throw new DietPlannerDomainException("You can only update recipes you created.");

        recipe.Update(command.Name, command.Description, command.Instructions,
            command.Servings, command.PrepTimeMinutes);

        recipe.ClearIngredients();
        foreach (var ing in command.Ingredients)
            recipe.AddIngredient(RecipeIngredientId.New(), ProductId.From(ing.ProductId), ing.Amount, ing.Unit);

        _repository.Update(recipe);
        await _unitOfWork.CommitAsync(ct);
    }
}
