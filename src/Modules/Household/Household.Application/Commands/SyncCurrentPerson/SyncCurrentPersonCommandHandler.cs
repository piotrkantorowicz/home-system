namespace Household.Application.Commands.SyncCurrentPerson;

using Household.Application.Common;
using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class SyncCurrentPersonCommandHandler
    : ICommandHandler<SyncCurrentPersonCommand, Guid>
{
    private readonly IPersonRepository _persons;
    private readonly InvitationResolver _invitationResolver;
    private readonly IHouseholdUnitOfWork _unitOfWork;

    public SyncCurrentPersonCommandHandler(
        IPersonRepository persons,
        InvitationResolver invitationResolver,
        IHouseholdUnitOfWork unitOfWork)
        => (_persons, _invitationResolver, _unitOfWork) = (persons, invitationResolver, unitOfWork);

    public async Task<Guid> HandleAsync(SyncCurrentPersonCommand command, CancellationToken ct)
    {
        var email = PersonEmail.CreateOrNull(command.Email);

        var person = await _persons.GetByAuthSubjectAsync(command.AuthSubject, ct)
                     ?? await TryLinkManagedPersonAsync(command.AuthSubject, email, ct);

        if (person is not null)
        {
            person.RefreshProfile(command.DisplayName, email, command.AvatarUrl);
        }
        else
        {
            person = Person.RegisterFromLogin(
                PersonId.New(), command.AuthSubject, command.DisplayName, email, command.AvatarUrl);
            await _persons.AddAsync(person, ct);
        }

        await _invitationResolver.TryResolveForAsync(person, ct);
        await _unitOfWork.CommitAsync(ct);

        return person.Id.Value;
    }

    /// <summary>
    /// First login of someone an adult set up as a managed member and converted to an
    /// account (#220): match the login email to a managed, unlinked <c>Person</c> and link
    /// it, so their personal data — keyed on <c>PersonId</c> — carries over untouched.
    /// </summary>
    private async Task<Person?> TryLinkManagedPersonAsync(
        string authSubject, PersonEmail? email, CancellationToken ct)
    {
        if (email is null)
            return null;

        var candidate = await _persons.GetByEmailAsync(email, ct);
        if (candidate is not { IsManaged: true, IsLinked: false })
            return null;

        candidate.LinkAuthSubject(authSubject);
        return candidate;
    }
}
