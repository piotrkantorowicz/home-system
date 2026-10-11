namespace Investments.Domain.NetWorth;

using Investments.Domain.Fx;
using Investments.Domain.ValueObjects;

/// <summary>
/// Net worth on a date: for each wallet, its latest balance on or before the date, converted to PLN
/// at the rate for that date. A foreign balance without a rate is left out and marks the result
/// incomplete — never guessed.
/// </summary>
public sealed class NetWorthCalculator
{
    private readonly IReadOnlyList<NetWorthWallet> _wallets;
    private readonly Dictionary<WalletId, NetWorthBalance[]> _balances;
    private readonly FxRateBook _rates;

    /// <summary>Creates a calculator over a fixed set of wallets, their balances and rates.</summary>
    /// <param name="wallets">The wallets in scope.</param>
    /// <param name="balances">Their balances, any order; balances of other wallets are ignored.</param>
    /// <param name="rates">Rates covering the dates to value.</param>
    public NetWorthCalculator(IReadOnlyList<NetWorthWallet> wallets, IEnumerable<NetWorthBalance> balances, FxRateBook rates)
    {
        ArgumentNullException.ThrowIfNull(wallets);
        ArgumentNullException.ThrowIfNull(balances);
        _wallets = wallets;
        _rates = rates ?? throw new ArgumentNullException(nameof(rates));
        _balances = balances
            .GroupBy(b => b.WalletId)
            .ToDictionary(g => g.Key, g => g.OrderBy(b => b.AsOf).ToArray());
    }

    /// <summary>Net worth on <paramref name="date"/>.</summary>
    /// <param name="date">The reference date.</param>
    public NetWorthPoint ValueAt(DateOnly date)
    {
        List<WalletValue> values = [];
        foreach (var wallet in _wallets)
        {
            var balance = _balances.TryGetValue(wallet.Id, out var history) ? history.LastOrDefault(b => b.AsOf <= date) : null;
            values.Add(new WalletValue(wallet, balance, balance is null ? null : _rates.ToPln(balance.Amount, wallet.Currency, date)));
        }

        return new NetWorthPoint(
            date,
            values.Sum(v => v.Pln?.Amount ?? 0m),
            values.Any(v => v.Pln?.RateMissing == true),
            values);
    }

    /// <summary>
    /// The dates of a month-end series over <c>[from, to]</c>: every last day of a month in the range,
    /// plus <paramref name="to"/> itself when it is not a month end (the current month so far).
    /// </summary>
    /// <param name="from">First date, inclusive.</param>
    /// <param name="to">Last date, inclusive.</param>
    public static IReadOnlyList<DateOnly> MonthEnds(DateOnly from, DateOnly to)
    {
        List<DateOnly> dates = [];
        for (var end = EndOfMonth(from); end <= to; end = EndOfMonth(end.AddDays(1)))
            dates.Add(end);
        if (from <= to && EndOfMonth(to) != to)
            dates.Add(to);
        return dates;
    }

    private static DateOnly EndOfMonth(DateOnly date) => new(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
}
