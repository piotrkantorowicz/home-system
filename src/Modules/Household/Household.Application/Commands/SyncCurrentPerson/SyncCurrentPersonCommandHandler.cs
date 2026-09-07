namespace Household.Application.Commands.SyncCurrentPerson;

using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class SyncCurrentPersonCommandHandler
    : ICommandHandler<SyncCurrentPersonCommand, Guid>
{
    private readonly IPersonRepository _persons;
    private readonly IHouseholdUnitOfWork _unitOfWork;

    public SyncCurrentPersonCommandHandler(
        IPersonRepository persons,
        IHouseholdUnitOfWork unitOfWork)
        => (_persons, _unitOfWork) = (persons, unitOfWork);

    public async Task<Guid> HandleAsync(SyncCurrentPersonCommand command, CancellationToken ct)
    {
        var email = PersonEmail.CreateOrNull(command.Email);

        var existing = await _persons.GetByAuthSubjectAsync(command.AuthSubject, ct);
        if (existing is not null)
        {
            existing.RefreshProfile(command.DisplayName, email, command.AvatarUrl);
            await _unitOfWork.CommitAsync(ct);
            return existing.Id.Value;
        }

        var person = Person.RegisterFromLogin(
            PersonId.New(),
            command.AuthSubject,
            command.DisplayName,
            email,
            command.AvatarUrl);

        await _persons.AddAsync(person, ct);
        await _unitOfWork.CommitAsync(ct);

        return person.Id.Value;
    }
}
