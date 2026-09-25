namespace Household.Application.Commands.InvitePersonByEmail;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class InvitePersonByEmailCommandHandler(
    HouseholdAccessService access,
    HouseholdInvitationIssuer issuer,
    IHouseholdUnitOfWork unitOfWork)
    : ICommandHandler<InvitePersonByEmailCommand, InvitePersonByEmailResult>
{
    public async Task<InvitePersonByEmailResult> HandleAsync(
        InvitePersonByEmailCommand command, CancellationToken ct)
    {
        var (caller, household) = await access.RequireOwnerAsync(
            command.RequestingAuthSubject, command.HouseholdId, ct);

        var email = PersonEmail.Create(command.Email);

        var invitationId = await issuer.IssueAsync(
            household, knownTarget: null, email, command.Role, caller.Id, nickname: null, ct);
        await unitOfWork.CommitAsync(ct);

        return new InvitePersonByEmailResult(invitationId.Value);
    }
}
