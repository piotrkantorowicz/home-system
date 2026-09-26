namespace DietPlanner.Application.Commands.UpdateProduct;

using DietPlanner.Application.Households;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateProductCommandHandler(
    IProductRepository repository,
    HouseholdRosterProvider households,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<UpdateProductCommand>
{
    public async Task HandleAsync(UpdateProductCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var product = await repository.GetByIdAsync(ProductId.From(command.Id), ct)
            ?? throw new NotFoundException("Product", command.Id);

        LibraryAccess access = await households.GetLibraryAccessAsync(command.UserId, ct);
        access.DemandEdit(product.CreatedByUserId, product.Visibility, "Product", command.Id);

        if (VisibilityInput.Parse(command.Visibility) is { } visibility && visibility != product.Visibility)
        {
            if (product.CreatedByUserId != command.UserId)
                throw new ForbiddenException("Only the creator can change who sees this product.");
            product.ChangeVisibility(visibility);
        }

        var nutrition = new NutritionPer100g(command.Calories, command.Protein, command.Carbs, command.Fat, command.Fiber);
        product.Update(command.Name, nutrition, command.DefaultUnit, command.DensityGramsPerMl, command.GramPerPiece, now);

        repository.Update(product);
        await unitOfWork.CommitAsync(ct);
    }
}
