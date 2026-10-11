namespace Investments.Application.Common;

using System.Data.Common;
using Investments.Domain.Abstractions;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;

internal static class WalletCommandExtensions
{
    private const string UniqueViolation = "23505";

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

    /// <summary>
    /// Commits, turning a lost race — a stale concurrency token, or a parallel insert of the same
    /// unique key — into a 409.
    /// </summary>
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
        catch (DbUpdateException ex) when (ex.InnerException is DbException { SqlState: UniqueViolation })
        {
            throw new ConflictException("This was changed by someone else at the same time. Reload and try again.");
        }
    }
}
