namespace DietPlanner.Application.Commands.CreateProduct;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task<Guid> HandleAsync(CreateProductCommand command, CancellationToken ct = default)
    {
        var existing = await _repository.GetByNameAsync(command.Name, command.UserId, ct);
        if (existing is not null)
            throw new DietPlanner.Domain.Exceptions.DietPlannerDomainException(
                $"A product with the name '{command.Name}' already exists.");

        var id = ProductId.New();
        var nutrition = new NutritionPer100g(command.Calories, command.Protein, command.Carbs, command.Fat, command.Fiber);
        var product = Product.Create(id, command.Name, nutrition, command.DefaultUnit,
            command.DensityGramsPerMl, command.GramPerPiece, command.UserId);

        await _repository.AddAsync(product, ct);
        await _unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
