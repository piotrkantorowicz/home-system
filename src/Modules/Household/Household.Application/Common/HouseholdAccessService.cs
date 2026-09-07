using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

namespace Household.Application.Common;

/// <summary>
/// Resolves the calling <see cref="Person"/> from its Authentik subject and enforces the
/// membership / owner checks that every household command shares.
/// </summary>
internal sealed class HouseholdAccessService
{
    private readonly IPersonRepository _persons;
    private readonly IHouseholdRepository _households;

    public HouseholdAccessService(IPersonRepository persons, IHouseholdRepository households)
        => (_persons, _households) = (persons, households);

    public async Task<Person> RequirePersonAsync(string authSubject, CancellationToken ct)
        => await _persons.GetByAuthSubjectAsync(authSubject, ct)
           ?? throw new NotFoundException(
               "No Person exists for the current account yet. Sync it via POST /api/persons/me/sync first.");

    public async Task<(Person Caller, HouseholdAggregate Household)> RequireMemberAsync(
        string authSubject, Guid householdId, CancellationToken ct)
    {
        var caller = await RequirePersonAsync(authSubject, ct);
        var household = await LoadAsync(householdId, ct);

        if (!household.HasMember(caller.Id))
            throw new ForbiddenException("You are not a member of this household.");

        return (caller, household);
    }

    public async Task<(Person Caller, HouseholdAggregate Household)> RequireOwnerAsync(
        string authSubject, Guid householdId, CancellationToken ct)
    {
        var (caller, household) = await RequireMemberAsync(authSubject, householdId, ct);

        if (household.RoleOf(caller.Id) != HouseholdRole.Owner)
            throw new ForbiddenException("Only an owner can perform this operation.");

        return (caller, household);
    }

    private async Task<HouseholdAggregate> LoadAsync(Guid householdId, CancellationToken ct)
        => await _households.GetByIdAsync(HouseholdId.From(householdId), ct)
           ?? throw new NotFoundException("Household", householdId);
}
