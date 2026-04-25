namespace DietPlanner.Application.Commands.OverrideMealEntry;

using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Core.Domain;

internal sealed class OverrideMealEntryCommandHandler : ICommandHandler<OverrideMealEntryCommand>
{
    private readonly IMealEntryRepository _repository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OverrideMealEntryCommandHandler(
        IMealEntryRepository repository,
        IRecipeRepository recipeRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _recipeRepository = recipeRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(OverrideMealEntryCommand command, CancellationToken ct = default)
    {
        var entry = await _repository.GetByIdAsync(MealEntryId.From(command.Id), ct)
            ?? throw new NotFoundException("MealEntry", command.Id);

        if (entry.UserId != command.UserId)
            throw new NotFoundException("MealEntry", command.Id);

        RecipeId? actualRecipeId = null;
        if (command.ActualRecipeId is { } rawRecipeId)
        {
            actualRecipeId = RecipeId.From(rawRecipeId);
            var recipe = await _recipeRepository.GetByIdAsync(actualRecipeId, ct);
            if (recipe is null || recipe.CreatedByUserId != command.UserId)
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
                .Where(p => p.CreatedByUserId == command.UserId)
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
