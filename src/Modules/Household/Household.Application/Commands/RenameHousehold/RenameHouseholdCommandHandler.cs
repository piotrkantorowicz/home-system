using Household.Application.Common;
using Household.Domain.Abstractions;
using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.RenameHousehold;

internal sealed class RenameHouseholdCommandHandler : ICommandHandler<RenameHouseholdCommand>
{
    private readonly HouseholdAccessService _access;
    private readonly IHouseholdUnitOfWork _unitOfWork;

    public RenameHouseholdCommandHandler(HouseholdAccessService access, IHouseholdUnitOfWork unitOfWork)
        => (_access, _unitOfWork) = (access, unitOfWork);

    public async Task HandleAsync(RenameHouseholdCommand command, CancellationToken ct)
    {
        var (_, household) = await _access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        household.Rename(command.Name);
        await _unitOfWork.CommitAsync(ct);
    }
}
