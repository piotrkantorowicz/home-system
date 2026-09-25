namespace Household.Application.Common;

using Household.Domain.Abstractions;
using Household.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Commits an invitation resolution (accept / decline / revoke) and translates a lost
/// optimistic-concurrency race into a business-rule violation. <c>HouseholdInvitation</c>
/// carries no in-memory lock, so two requests can both read it <c>Pending</c> and both try to
/// resolve it; the EF concurrency token (see <c>HouseholdInvitationConfiguration</c>) lets only the
/// first write through — the loser hits <see cref="DbUpdateConcurrencyException"/> here instead of
/// silently overwriting the winner's outcome.
/// </summary>
internal static class InvitationConflictGuard
{
    public static async Task CommitOrThrowConflictAsync(this IHouseholdUnitOfWork unitOfWork, CancellationToken ct)
    {
        try
        {
            await unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new HouseholdDomainException(
                "This invitation was already resolved by someone else. Refresh and try again.");
        }
    }
}
