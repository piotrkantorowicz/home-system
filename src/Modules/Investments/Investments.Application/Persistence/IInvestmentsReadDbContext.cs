namespace Investments.Application.Persistence;

using Investments.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Read-side surface over the module's <c>DbContext</c>. Query handlers project with
/// <c>AsNoTracking()</c> + <c>Select()</c> — they never load aggregates.
/// </summary>
public interface IInvestmentsReadDbContext
{
    /// <summary>Every wallet, in every household.</summary>
    DbSet<Wallet> Wallets { get; }

    /// <summary>Every balance snapshot, in every household.</summary>
    DbSet<BalanceSnapshot> BalanceSnapshots { get; }
}
