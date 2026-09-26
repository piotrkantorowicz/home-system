namespace DietPlanner.Application.Commands.DeleteProduct;

using DietPlanner.Application.Households;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class DeleteProductCommandHandler(
    IProductRepository repository,
    HouseholdRosterProvider households,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<DeleteProductCommand>
{
    public async Task HandleAsync(DeleteProductCommand command, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var product = await repository.GetByIdAsync(ProductId.From(command.Id), ct)
            ?? throw new NotFoundException("Product", command.Id);

        LibraryAccess access = await households.GetLibraryAccessAsync(command.UserId, ct);
        access.DemandEdit(product.CreatedByUserId, product.Visibility, "Product", command.Id);

        product.SoftDelete(now);
        repository.Update(product);
        await unitOfWork.CommitAsync(ct);
    }
}
