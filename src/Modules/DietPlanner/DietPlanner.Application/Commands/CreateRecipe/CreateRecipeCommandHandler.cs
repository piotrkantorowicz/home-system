namespace DietPlanner.Application.Commands.CreateRecipe;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Domain;

internal sealed class CreateRecipeCommandHandler : ICommandHandler<CreateRecipeCommand, Guid>
{
    private readonly IRecipeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRecipeCommandHandler(IRecipeRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task<Guid> HandleAsync(CreateRecipeCommand command, CancellationToken ct = default)
    {
        var existing = await _repository.GetByNameAsync(command.Name, command.UserId, ct);
        if (existing is not null)
            throw new DietPlanner.Domain.Exceptions.DietPlannerDomainException(
                $"A recipe with the name '{command.Name}' already exists.");

        var id = RecipeId.New();
        var recipe = Recipe.Create(id, command.Name, command.Description, command.Instructions,
            command.Servings, command.PrepTimeMinutes, command.UserId);

        foreach (var ing in command.Ingredients)
            recipe.AddIngredient(RecipeIngredientId.New(), ProductId.From(ing.ProductId), ing.Amount, ing.Unit);

        await _repository.AddAsync(recipe, ct);
        await _unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
