namespace DietPlanner.Application.Commands.OverrideMealEntry;

using DietPlanner.Application.Households;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class OverrideMealEntryCommandHandler : ICommandHandler<OverrideMealEntryCommand>
{
    private readonly IMealEntryRepository _repository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HouseholdRosterProvider _households;

    public OverrideMealEntryCommandHandler(
        IMealEntryRepository repository,
        IRecipeRepository recipeRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        HouseholdRosterProvider households)
    {
        _repository = repository;
        _recipeRepository = recipeRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _households = households;
    }

    public async Task HandleAsync(OverrideMealEntryCommand command, CancellationToken ct = default)
    {
        var entry = await _repository.GetByIdAsync(MealEntryId.From(command.Id), ct)
            ?? throw new NotFoundException("MealEntry", command.Id);

        HouseholdRoster roster = await _households.GetAsync(command.PersonId, command.AuthSubject, ct);
        roster.Demand(entry.PersonId, roster.CanLogFor(entry.PersonId), "MealEntry", command.Id);

        RecipeId? actualRecipeId = null;
        if (command.ActualRecipeId is { } rawRecipeId)
        {
            actualRecipeId = RecipeId.From(rawRecipeId);
            var recipe = await _recipeRepository.GetByIdAsync(actualRecipeId, ct);
            if (recipe is null || recipe.CreatedByUserId != command.AuthSubject)
                throw new NotFoundException("Recipe", rawRecipeId);
        }

        var productInputs = command.ActualProducts
            .Select(p => (ProductId: ProductId.From(p.ProductId), p.Amount, p.Unit))
            .ToList();

        if (productInputs.Count > 0)
        {
            var productIds = productInputs.Select(p => p.ProductId).Distinct().ToList();
            var products = await _productRepository.GetByIdsAsync(productIds, ct);
            var foundIds = products
                .Where(p => p.CreatedByUserId == command.AuthSubject)
                .Select(p => p.Id)
                .ToHashSet();

            foreach (var input in productInputs)
            {
                if (!foundIds.Contains(input.ProductId))
                    throw new NotFoundException("Product", input.ProductId.Value);
            }
        }

        entry.ApplyOverride(actualRecipeId, productInputs);
        _repository.Update(entry);
        await _unitOfWork.CommitAsync(ct);
    }
}
