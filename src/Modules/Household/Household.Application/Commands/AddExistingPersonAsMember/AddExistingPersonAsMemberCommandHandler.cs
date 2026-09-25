namespace Household.Application.Commands.AddExistingPersonAsMember;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class AddExistingPersonAsMemberCommandHandler(
    HouseholdAccessService access,
    IPersonRepository persons,
    HouseholdInvitationIssuer issuer,
    IHouseholdUnitOfWork unitOfWork)
    : ICommandHandler<AddExistingPersonAsMemberCommand, AddExistingPersonAsMemberResult>
{
    public async Task<AddExistingPersonAsMemberResult> HandleAsync(
        AddExistingPersonAsMemberCommand command, CancellationToken ct)
    {
        var (caller, household) = await access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var targetId = PersonId.From(command.PersonId);
        var target = await persons.GetByIdAsync(targetId, ct)
            ?? throw new NotFoundException("Person", command.PersonId);

        var invitationId = await issuer.IssueAsync(
            household, target, target.Email, command.Role, caller.Id, command.Nickname, ct);
        await unitOfWork.CommitAsync(ct);

        return new AddExistingPersonAsMemberResult(invitationId.Value);
    }
}
