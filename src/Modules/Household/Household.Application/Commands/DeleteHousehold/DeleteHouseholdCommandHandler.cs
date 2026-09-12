namespace Household.Application.Commands.DeleteHousehold;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Shared.Abstractions.Cqrs;

internal sealed class DeleteHouseholdCommandHandler : ICommandHandler<DeleteHouseholdCommand>
{
    private readonly HouseholdAccessService _access;
    private readonly IHouseholdRepository _households;
    private readonly IHouseholdUnitOfWork _unitOfWork;

    public DeleteHouseholdCommandHandler(
        HouseholdAccessService access,
        IHouseholdRepository households,
        IHouseholdUnitOfWork unitOfWork)
        => (_access, _households, _unitOfWork) = (access, households, unitOfWork);

    public async Task HandleAsync(DeleteHouseholdCommand command, CancellationToken ct)
    {
        var (_, household) = await _access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        _households.Remove(household);
        await _unitOfWork.CommitAsync(ct);
    }
}
