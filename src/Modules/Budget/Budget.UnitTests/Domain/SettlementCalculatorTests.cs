namespace Budget.UnitTests.Domain;

using global::Budget.Domain.Services;

/// <summary>Exact, deterministic settlement arithmetic.</summary>
public sealed class SettlementCalculatorTests
{
    private static Guid P(int n) => new(n, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0]);

    private static IReadOnlyDictionary<Guid, long> Net(
        (Guid, long)[]? payments = null, (Guid, long)[]? shares = null, (Guid, Guid, long)[]? repayments = null)
        => SettlementCalculator.Net(payments ?? [], shares ?? [], repayments ?? []);

    /// <summary>The design's worked example, step by step, including the repayment; private and household-funded rows add nothing.</summary>
    [Fact]
    public void DesignExample_ReproducesEveryRow()
    {
        var alex = P(1);
        var bea = P(2);

        // Alex pays 120.00, split 60.00 each.
        var row1 = Net([(alex, 12000)], [(alex, 6000), (bea, 6000)]);
        row1[alex].ShouldBe(6000);
        row1[bea].ShouldBe(-6000);

        // Bea pays 40.00, split 20.00 each.
        var row2 = Net([(alex, 12000), (bea, 4000)], [(alex, 6000), (bea, 6000), (alex, 2000), (bea, 2000)]);
        row2[alex].ShouldBe(4000);
        row2[bea].ShouldBe(-4000);

        // Bea repays Alex 25.00 → 15.00 still owed. Household funds and private spending are not inputs at all.
        var row3 = Net(
            [(alex, 12000), (bea, 4000)], [(alex, 6000), (bea, 6000), (alex, 2000), (bea, 2000)], [(bea, alex, 2500)]);
        row3[alex].ShouldBe(1500);
        row3[bea].ShouldBe(-1500);
        row3.Values.Sum().ShouldBe(0);

        // Void of the repayment simply removes it: back to 40.00.
        Net([(alex, 12000), (bea, 4000)], [(alex, 6000), (bea, 6000), (alex, 2000), (bea, 2000)])[alex].ShouldBe(4000);
    }

    /// <summary>An overpayment becomes a visible reverse credit rather than being rejected.</summary>
    [Fact]
    public void Overpayment_CreatesReverseCredit()
    {
        var net = Net([(P(1), 4000)], [(P(1), 2000), (P(2), 2000)], [(P(2), P(1), 3000)]);

        net[P(2)].ShouldBe(1000);
        net[P(1)].ShouldBe(-1000);
    }

    /// <summary>No entries, or already balanced entries, suggest nothing.</summary>
    [Fact]
    public void Suggest_NothingToSettle()
    {
        SettlementCalculator.Suggest(Net()).ShouldBeEmpty();
        SettlementCalculator.Suggest(Net([(P(1), 1000)], [(P(1), 1000)])).ShouldBeEmpty();
    }

    /// <summary>One debtor and one creditor produce a single transfer of the whole debt.</summary>
    [Fact]
    public void Suggest_OneDebtorOneCreditor()
    {
        var transfers = SettlementCalculator.Suggest(Net([(P(1), 1000)], [(P(2), 1000)]));

        transfers.Count.ShouldBe(1);
        (transfers[0].From, transfers[0].To, transfers[0].MinorUnits).ShouldBe((P(2), P(1), 1000));
    }

    /// <summary>With several debtors the largest is matched first, so recipients may differ from the original payer.</summary>
    [Fact]
    public void Suggest_MultipleDebtors_LargestFirst_ConservesEveryCent()
    {
        // Alex paid 90.00 shared three ways; Bea owes 30.00, Cy owes 30.00 — but also Cy paid 10.00 for Bea alone.
        var net = Net([(P(1), 9000), (P(3), 1000)], [(P(1), 3000), (P(2), 3000), (P(3), 3000), (P(2), 1000)]);

        var transfers = SettlementCalculator.Suggest(net);

        Apply(net, transfers).Values.ShouldAllBe(v => v == 0);
        transfers.Sum(t => t.MinorUnits).ShouldBe(net.Values.Where(v => v > 0).Sum());
    }

    /// <summary>Ties between equal magnitudes are broken by the lower person ID, every time.</summary>
    [Fact]
    public void Suggest_TieOrdering_IsDeterministic()
    {
        var net = Net([(P(1), 1000), (P(2), 1000)], [(P(3), 1000), (P(4), 1000)]);

        var transfers = SettlementCalculator.Suggest(net);

        transfers.Select(t => (t.From, t.To, t.MinorUnits)).ShouldBe([(P(3), P(1), 1000), (P(4), P(2), 1000)]);
        SettlementCalculator.Suggest(net).ShouldBe(transfers);
    }

    /// <summary>Uneven cents survive: 100.00 split three ways leaves penny-exact balances and exact transfers.</summary>
    [Fact]
    public void Suggest_UnevenCents_AreExact()
    {
        var net = Net([(P(1), 10000)], [(P(1), 3334), (P(2), 3333), (P(3), 3333)]);

        net[P(1)].ShouldBe(6666);
        var transfers = SettlementCalculator.Suggest(net);

        transfers.Select(t => t.MinorUnits).Order().ShouldBe([3333, 3333]);
        Apply(net, transfers).Values.ShouldAllBe(v => v == 0);
    }

    /// <summary>Whatever the input, net sums to zero and applying all suggestions settles everyone.</summary>
    [Fact]
    public void RandomLedgers_ConserveAndSettle()
    {
        var random = new Random(453);
        for (var round = 0; round < 200; round++)
        {
            var people = Enumerable.Range(1, random.Next(2, 7)).Select(P).ToArray();
            var payments = new List<(Guid, long)>();
            var shares = new List<(Guid, long)>();
            var repayments = new List<(Guid, Guid, long)>();
            for (var i = 0; i < random.Next(1, 12); i++)
            {
                var total = random.Next(1, 100_000);
                var sharers = people.OrderBy(_ => random.Next()).Take(random.Next(1, people.Length + 1)).ToArray();
                payments.Add((people[random.Next(people.Length)], total));
                var each = total / sharers.Length;
                var rest = total % sharers.Length;
                for (var s = 0; s < sharers.Length; s++)
                    shares.Add((sharers[s], each + (s < rest ? 1 : 0)));
            }

            for (var i = 0; i < random.Next(0, 4); i++)
            {
                var from = people[random.Next(people.Length)];
                var to = people[random.Next(people.Length)];
                if (from != to) repayments.Add((from, to, random.Next(1, 50_000)));
            }

            var net = SettlementCalculator.Net(payments, shares, repayments);
            net.Values.Sum().ShouldBe(0);
            Apply(net, SettlementCalculator.Suggest(net)).Values.ShouldAllBe(v => v == 0);
        }
    }

    private static Dictionary<Guid, long> Apply(IReadOnlyDictionary<Guid, long> net, IEnumerable<global::Budget.Domain.ValueObjects.Transfer> transfers)
    {
        var result = net.ToDictionary(kv => kv.Key, kv => kv.Value);
        foreach (var t in transfers)
        {
            result[t.From] += t.MinorUnits;
            result[t.To] -= t.MinorUnits;
        }

        return result;
    }
}
