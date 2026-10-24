namespace DietPlanner.Application.Commands.CreateRecipe;

using DietPlanner.Application.Households;
using DietPlanner.Application.OperationReceipts;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class CreateRecipeCommandHandler(
    IRecipeRepository repository,
    IProductRepository productRepository,
    HouseholdRosterProvider households,
    OperationReplay replay,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreateRecipeCommand, Guid>
{
    private const string Operation = "create_recipe";

    public async Task<Guid> HandleAsync(CreateRecipeCommand command, CancellationToken ct = default)
    {
        LibraryAccess library = await households.GetLibraryAccessAsync(command.UserId, ct);
        library.DemandWrite();
        var productIds = command.Ingredients.Select(i => ProductId.From(i.ProductId)).Distinct().ToList();
        library.DemandReadable(await productRepository.GetByIdsAsync(productIds, ct), productIds);

        var now = clock.GetUtcNow().UtcDateTime;
        var id = RecipeId.New();

        if (command.OperationKey is { } key)
        {
            // The fields that define "the same recipe"; the key and caller are scoped by the receipt itself.
            var canonical = new
            {
                command.Name,
                command.Description,
                command.Instructions,
                command.Servings,
                command.PrepTimeMinutes,
                command.Ingredients,
                command.Visibility,
            };
            if (await replay.ClaimOrReplayAsync(command.UserId, Operation, key, canonical, id.Value, now, ct) is { } original)
                return original;
        }

        var existing = await repository.GetByNameAsync(command.Name, command.UserId, ct);
        if (existing is not null)
            throw new DietPlanner.Domain.Exceptions.DietPlannerDomainException(
                $"A recipe with the name '{command.Name}' already exists.");

        var recipe = Recipe.Create(id, command.Name, command.Description, command.Instructions,
            command.Servings, command.PrepTimeMinutes, command.UserId, now,
            VisibilityInput.Parse(command.Visibility) ?? Visibility.Household);

        foreach (var ing in command.Ingredients)
            recipe.AddIngredient(RecipeIngredientId.New(), ProductId.From(ing.ProductId), ing.Amount, ing.Unit);

        await repository.AddAsync(recipe, ct);
        await unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
