namespace Investments.Application.Common;

using Investments.Application.Persistence;
using Investments.Domain.NetWorth;
using Microsoft.EntityFrameworkCore;

internal static class NetWorthLoader
{
    /// <summary>
    /// Loads the in-scope wallets, their balances up to <paramref name="to"/> and the rates for
    /// <c>[from, to]</c> — or only today's rates when <paramref name="latestRates"/>. Small by design
    /// (≈10 wallets × ≈120 months); computed in memory.
    /// </summary>
    public static async Task<(NetWorthCalculator Calculator, IReadOnlyList<NetWorthWalletRow> Wallets)> LoadNetWorthAsync(
        this IInvestmentsReadDbContext db, InvestmentsCaller caller, string scope, DateOnly from, DateOnly to, CancellationToken ct,
        bool latestRates = false)
    {
        var walletsInScope = db.Wallets.AsNoTracking().InScope(caller, scope);
        var wallets = await walletsInScope
            .OrderBy(w => w.Name).ThenBy(w => w.Id)
            .Select(w => new NetWorthWalletRow(w.Id, w.Name, w.Kind, w.Currency, w.OwnerPersonId, w.OwnerName, w.ArchivedOn))
            .ToListAsync(ct);
        var balances = await db.BalanceSnapshots.AsNoTracking()
            .Where(s => s.AsOf <= to && walletsInScope.Any(w => w.Id == s.WalletId))
            .Select(s => new NetWorthBalance(s.WalletId, s.AsOf, s.Amount))
            .ToListAsync(ct);
        var rates = latestRates
            ? await db.LoadLatestRatesAsync(wallets.Select(w => w.Currency), ct)
            : await db.LoadRatesAsync(wallets.SelectMany(w => new[] { (w.Currency, from), (w.Currency, to) }), ct);

        var calculator = new NetWorthCalculator(
            [.. wallets.Select(w => w.ToNetWorthWallet())], balances, rates);
        return (calculator, wallets);
    }
}
