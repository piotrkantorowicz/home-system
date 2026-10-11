namespace Investments.Application.Common;

using Investments.Domain.Abstractions;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;

internal static class WalletCommandExtensions
{
    /// <summary>
    /// Loads a wallet the caller may edit. Missing, other-household and invisible wallets are all
    /// not-found, so existence never leaks.
    /// </summary>
    public static async Task<Wallet> LoadAccessibleAsync(
        this IWalletRepository wallets, InvestmentsCaller caller, Guid id, CancellationToken ct)
    {
        var wallet = await wallets.GetAsync(WalletId.From(id), caller.HouseholdId, ct);

        return wallet is not null && caller.CanAccess(wallet)
            ? wallet
            : throw new NotFoundException("Wallet", id);
    }

    /// <summary>Commits, turning a lost race on a row's concurrency token into a 409.</summary>
    public static async Task CommitOrThrowConflictAsync(this IInvestmentsUnitOfWork unitOfWork, CancellationToken ct)
    {
        try
        {
            await unitOfWork.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This wallet was changed by someone else. Reload and try again.");
        }
    }
}
