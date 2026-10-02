namespace Budget.Domain.Services;

using global::Budget.Domain.ValueObjects;

/// <summary>
/// Pure, exact settlement arithmetic in minor units. For each person:
/// <c>net = personal payments − assigned shares + repayments sent − repayments received</c>;
/// positive means the person should receive money, negative means they owe. Nothing here knows
/// about dates, roles or rosters — callers pass every active recorded entry, so there is no cutoff.
/// </summary>
public static class SettlementCalculator
{
    /// <summary>Net minor-unit balance per person; the values always sum to zero for consistent input.</summary>
    /// <param name="payments">Individual-funded shared expenses: who paid how much.</param>
    /// <param name="shares">Stored shares: who owes how much.</param>
    /// <param name="repayments">Recorded payments: who actually paid whom how much.</param>
    public static IReadOnlyDictionary<Guid, long> Net(
        IEnumerable<(Guid Person, long Minor)> payments,
        IEnumerable<(Guid Person, long Minor)> shares,
        IEnumerable<(Guid From, Guid To, long Minor)> repayments)
    {
        var net = new Dictionary<Guid, long>();
        void Add(Guid person, long minor) => net[person] = net.GetValueOrDefault(person) + minor;

        foreach (var (person, minor) in payments) Add(person, minor);
        foreach (var (person, minor) in shares) Add(person, -minor);
        foreach (var (from, to, minor) in repayments)
        {
            Add(from, minor);
            Add(to, -minor);
        }

        return net;
    }

    /// <summary>
    /// Deterministic suggestions: repeatedly match the largest remaining debtor with the largest
    /// remaining creditor (ties by lower <c>PersonId</c>) and transfer the smaller of the two. Every
    /// cent is conserved and applying all transfers brings every balance to zero. It does not
    /// minimise the number of transfers.
    /// </summary>
    /// <param name="net">Balances from <see cref="Net"/>.</param>
    public static IReadOnlyList<Transfer> Suggest(IReadOnlyDictionary<Guid, long> net)
    {
        var debtors = net.Where(kv => kv.Value < 0).ToDictionary(kv => kv.Key, kv => -kv.Value);
        var creditors = net.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
        var transfers = new List<Transfer>();

        while (debtors.Count > 0 && creditors.Count > 0)
        {
            var debtor = Largest(debtors);
            var creditor = Largest(creditors);
            var amount = Math.Min(debtors[debtor], creditors[creditor]);
            transfers.Add(new Transfer(debtor, creditor, amount));

            Reduce(debtors, debtor, amount);
            Reduce(creditors, creditor, amount);
        }

        return transfers;
    }

    private static Guid Largest(Dictionary<Guid, long> remaining)
        => remaining.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First().Key;

    private static void Reduce(Dictionary<Guid, long> remaining, Guid person, long amount)
    {
        if ((remaining[person] -= amount) == 0)
            remaining.Remove(person);
    }
}
