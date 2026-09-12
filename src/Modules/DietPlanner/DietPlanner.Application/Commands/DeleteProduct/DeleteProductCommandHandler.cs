namespace DietPlanner.Application.Commands.DeleteProduct;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(DeleteProductCommand command, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(ProductId.From(command.Id), ct)
            ?? throw new NotFoundException("Product", command.Id);

        if (product.CreatedByUserId != command.UserId)
            throw new DietPlannerDomainException("You can only delete products you created.");

        product.SoftDelete();
        _repository.Update(product);
        await _unitOfWork.CommitAsync(ct);
    }
}
