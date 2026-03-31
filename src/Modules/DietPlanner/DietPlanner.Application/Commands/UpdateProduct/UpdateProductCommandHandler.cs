namespace DietPlanner.Application.Commands.UpdateProduct;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.CQRS;
using Shared.Abstractions.Domain;

internal sealed class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(UpdateProductCommand command, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(ProductId.From(command.Id), ct)
            ?? throw new NotFoundException("Product", command.Id);

        if (product.CreatedByUserId != command.UserId)
            throw new DietPlannerDomainException("You can only update products you created.");

        var nutrition = new NutritionPer100g(command.Calories, command.Protein, command.Carbs, command.Fat, command.Fiber);
        product.Update(command.Name, nutrition, command.DefaultUnit, command.DensityGramsPerMl, command.GramPerPiece);

        _repository.Update(product);
        await _unitOfWork.CommitAsync(ct);
    }
}
