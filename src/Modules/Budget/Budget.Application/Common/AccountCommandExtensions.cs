namespace Budget.Application.Common;

using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;

internal static class AccountCommandExtensions
{
    /// <summary>
    /// Loads an envelope in the caller's budget for modification. Missing, other-household and
    /// invisible envelopes are all not-found, so existence never leaks.
    /// </summary>
    public static async Task<BudgetAccount> LoadManageableAsync(
        this IBudgetAccountRepository accounts, IBudgetRepository budgets, BudgetCaller caller, Guid id, CancellationToken ct)
    {
        var budget = await budgets.GetByHouseholdAsync(caller.HouseholdId, ct)
            ?? throw new NotFoundException("Envelope", id);
        var account = await accounts.GetAsync(BudgetAccountId.From(id), budget.Id, ct);

        return account is not null && caller.CanAccess(account)
            ? account
            : throw new NotFoundException("Envelope", id);
    }

    /// <summary>Commits, turning a lost race on the row's concurrency token into a 409.</summary>
    public static async Task CommitOrThrowConflictAsync(this IBudgetUnitOfWork unitOfWork, CancellationToken ct)
    {
        try
        {
            await unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This envelope was changed by someone else. Reload and try again.");
        }
    }
}
