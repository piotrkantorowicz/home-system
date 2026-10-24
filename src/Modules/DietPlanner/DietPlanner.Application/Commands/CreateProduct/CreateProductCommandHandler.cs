namespace DietPlanner.Application.Commands.CreateProduct;

using DietPlanner.Application.Households;
using DietPlanner.Application.OperationReceipts;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class CreateProductCommandHandler(
    IProductRepository repository,
    HouseholdRosterProvider households,
    OperationReplay replay,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreateProductCommand, Guid>
{
    private const string Operation = "create_product";

    public async Task<Guid> HandleAsync(CreateProductCommand command, CancellationToken ct = default)
    {
        (await households.GetLibraryAccessAsync(command.UserId, ct)).DemandWrite();

        var now = clock.GetUtcNow().UtcDateTime;
        var id = ProductId.New();
        var visibility = VisibilityInput.Parse(command.Visibility) ?? Visibility.Household;

        if (command.OperationKey is { } key)
        {
            // The fields that define "the same product"; the key and caller are scoped by the receipt itself.
            var canonical = new
            {
                command.Name,
                command.Calories,
                command.Protein,
                command.Carbs,
                command.Fat,
                command.Fiber,
                command.DefaultUnit,
                command.DensityGramsPerMl,
                command.GramPerPiece,
                Visibility = visibility.ToString(),
            };
            if (await replay.ClaimOrReplayAsync(command.UserId, Operation, key, canonical, id.Value, now, ct) is { } original)
                return original;
        }

        var existing = await repository.GetByNameAsync(command.Name, command.UserId, ct);
        if (existing is not null)
            throw new DietPlanner.Domain.Exceptions.DietPlannerDomainException(
                $"A product with the name '{command.Name}' already exists.");

        var nutrition = new NutritionPer100g(command.Calories, command.Protein, command.Carbs, command.Fat, command.Fiber);
        var product = Product.Create(id, command.Name, nutrition, command.DefaultUnit,
            command.DensityGramsPerMl, command.GramPerPiece, command.UserId, now,
            visibility);

        await repository.AddAsync(product, ct);
        await unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
