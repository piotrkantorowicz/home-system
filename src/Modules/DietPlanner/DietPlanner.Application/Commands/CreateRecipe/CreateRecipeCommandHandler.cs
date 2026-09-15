namespace DietPlanner.Application.Commands.CreateRecipe;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class CreateRecipeCommandHandler(
    IRecipeRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreateRecipeCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateRecipeCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var existing = await repository.GetByNameAsync(command.Name, command.UserId, ct);
        if (existing is not null)
            throw new DietPlanner.Domain.Exceptions.DietPlannerDomainException(
                $"A recipe with the name '{command.Name}' already exists.");

        var id = RecipeId.New();
        var recipe = Recipe.Create(id, command.Name, command.Description, command.Instructions,
            command.Servings, command.PrepTimeMinutes, command.UserId, now);

        foreach (var ing in command.Ingredients)
            recipe.AddIngredient(RecipeIngredientId.New(), ProductId.From(ing.ProductId), ing.Amount, ing.Unit);

        await repository.AddAsync(recipe, ct);
        await unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
