namespace DietPlanner.Application.Commands.DeleteProduct;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class DeleteProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<DeleteProductCommand>
{
    public async Task HandleAsync(DeleteProductCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var product = await repository.GetByIdAsync(ProductId.From(command.Id), ct)
            ?? throw new NotFoundException("Product", command.Id);

        if (product.CreatedByUserId != command.UserId)
            throw new DietPlannerDomainException("You can only delete products you created.");

        product.SoftDelete(now);
        repository.Update(product);
        await unitOfWork.CommitAsync(ct);
    }
}
