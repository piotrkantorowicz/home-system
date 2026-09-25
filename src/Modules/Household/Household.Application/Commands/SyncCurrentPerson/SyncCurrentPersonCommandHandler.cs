namespace Household.Application.Commands.SyncCurrentPerson;

using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class SyncCurrentPersonCommandHandler(
    IPersonRepository persons,
    IHouseholdUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<SyncCurrentPersonCommand, Guid>
{
    public async Task<Guid> HandleAsync(SyncCurrentPersonCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var email = PersonEmail.CreateOrNull(command.Email);

        var person = await persons.GetByAuthSubjectAsync(command.AuthSubject, ct)
                     ?? await TryLinkManagedPersonAsync(command.AuthSubject, email, now, ct);

        if (person is not null)
        {
            person.RefreshProfile(command.DisplayName, email, command.AvatarUrl, now);
        }
        else
        {
            person = Person.RegisterFromLogin(
                PersonId.New(), command.AuthSubject, command.DisplayName, email, command.AvatarUrl, now);
            await persons.AddAsync(person, ct);
        }

        await unitOfWork.CommitAsync(ct);

        return person.Id.Value;
    }

    /// <summary>
    /// First login of someone an adult set up as a managed member and converted to an
    /// account (#220): match the login email to a managed, unlinked <c>Person</c> and link
    /// it, so their personal data — keyed on <c>PersonId</c> — carries over untouched.
    /// </summary>
    private async Task<Person?> TryLinkManagedPersonAsync(
        string authSubject, PersonEmail? email, DateTime now, CancellationToken ct)
    {
        if (email is null)
            return null;

        var candidate = await persons.GetByEmailAsync(email, ct);
        if (candidate is not { IsManaged: true, IsLinked: false })
            return null;

        candidate.LinkAuthSubject(authSubject, now);
        return candidate;
    }
}
