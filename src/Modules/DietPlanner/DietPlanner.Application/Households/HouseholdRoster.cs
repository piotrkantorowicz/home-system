namespace DietPlanner.Application.Households;

using Household.Contracts.Interfaces;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// The caller's view of their household for DietPlanner access rules (design §3): who they may
/// read, plan for and log personal data for. A caller without a household is alone in it and
/// may do everything for themselves.
/// </summary>
/// <param name="CallerPersonId">Person identifier of the caller.</param>
/// <param name="CallerRole">The caller's household role, or <see langword="null"/> without a household.</param>
/// <param name="Members">Every household member, the caller included; empty without a household.</param>
internal sealed record HouseholdRoster(
    Guid CallerPersonId,
    string? CallerRole,
    IReadOnlyList<HouseholdContextMember> Members)
{
    // Role names as HouseholdContext carries them (Household.Domain HouseholdRole.ToString()).
    private const string Owner = "Owner";
    private const string Adult = "Adult";
    private const string Guest = "Guest";

    private bool CallerIsAdult => CallerRole is null or Owner or Adult;

    /// <summary>True when <paramref name="personId"/> is the caller or one of their household members.</summary>
    public bool IsMember(Guid personId)
        => personId == CallerPersonId || Members.Any(m => m.PersonId == personId);

    /// <summary>Display name of a household member, or <see langword="null"/> without a household.</summary>
    public string? NameOf(Guid personId)
        => Members.FirstOrDefault(m => m.PersonId == personId)?.DisplayName;

    /// <summary>The meal plan is shared: Owner/Adult plan for anyone, Child for themselves, Guest for no one.</summary>
    public bool CanPlanFor(Guid personId)
        => personId == CallerPersonId
            ? CallerRole != Guest
            : CallerIsAdult && IsMember(personId);

    /// <summary>Personal data (meal actuals, schedule): the person themselves, or an Owner/Adult for a managed member.</summary>
    public bool CanLogFor(Guid personId)
        => personId == CallerPersonId
            ? CallerRole != Guest
            : CallerIsAdult && Members.Any(m => m.PersonId == personId && m.IsManaged);

    /// <summary>Throws 404 for a person outside the household, 403 when <paramref name="allowed"/> is false.</summary>
    public void Demand(Guid personId, bool allowed, string resource, Guid resourceId)
    {
        if (!IsMember(personId))
            throw new NotFoundException(resource, resourceId);
        if (!allowed)
            throw new ForbiddenException($"Your household role does not allow changing this {resource}.");
    }
}
