namespace Budget.UnitTests.Application.StatementImports;

using global::Budget.Application.StatementImports;

/// <summary>#671 — source keys (§4.1), candidate rules (§3) and the proposed description (§5).</summary>
public sealed class StatementSourceKeyTests
{
    private static StatementTransaction Row(
        decimal amount = -10m, string title = "SKLEP", string? reference = null, string? account = "00 1140", string type = "ZAKUP PRZY UŻYCIU KARTY",
        bool booked = true, bool foreign = false, string counterparty = "", int line = 1)
        => new(line, account, new(2026, 10, 20), new(2026, 10, 19), null, amount, foreign ? "EUR" : "PLN", foreign, counterparty, title, reference, type, booked);

    private static readonly Guid Alex = Guid.CreateVersion7();

    private static string Key(StatementTransaction row) => StatementSourceKey.ForAll("mbank", Alex, [row]).Single();

    /// <summary>The same row always hashes to the same 64-character lowercase hex key.</summary>
    [Fact]
    public void ForAll_SameRow_IsDeterministicLowercaseHex()
    {
        var key = Key(Row());

        key.ShouldBe(Key(Row(line: 99)));
        key.ShouldMatch("^[0-9a-f]{64}$");
    }

    /// <summary>Two identical purchases on one day stay two rows: the occurrence number separates them.</summary>
    [Fact]
    public void ForAll_IdenticalRows_GetDistinctKeys()
    {
        var keys = StatementSourceKey.ForAll("mbank", Alex, [Row(), Row(), Row(title: "OTHER")]);

        keys.Distinct().Count().ShouldBe(3);
        keys[0].ShouldBe(Key(Row()));
    }

    /// <summary>The fingerprint ignores case, spacing and card masks, but not amount, account or profile.</summary>
    [Fact]
    public void ForAll_Fingerprint_NormalisesDescriptionOnly()
    {
        Key(Row(title: "sklep   Nr karty 4246xx******1234")).ShouldBe(Key(Row()));
        Key(Row(amount: -10.01m)).ShouldNotBe(Key(Row()));
        Key(Row(account: "00 1140 2")).ShouldNotBe(Key(Row()));
        StatementSourceKey.ForAll("ing", Alex, [Row()]).Single().ShouldNotBe(Key(Row()));
    }

    /// <summary>
    /// An account-less row is keyed by the importing person, so two adults' identical purchases stay
    /// importable; a row with an account is the same whoever imports it.
    /// </summary>
    [Fact]
    public void ForAll_NoAccount_KeysByImporter()
    {
        StatementSourceKey.ForAll("pko-bp", Alex, [Row(account: null)]).ShouldNotBe(StatementSourceKey.ForAll("pko-bp", Guid.CreateVersion7(), [Row(account: null)]));
        StatementSourceKey.ForAll("pko-bp", Alex, [Row()]).ShouldBe(StatementSourceKey.ForAll("pko-bp", Guid.CreateVersion7(), [Row()]));
    }

    /// <summary>With a bank reference, the reference alone identifies the row; the text may change between exports.</summary>
    [Fact]
    public void ForAll_BankReference_IgnoresAmountAndText()
    {
        Key(Row(reference: "R1", title: "A")).ShouldBe(Key(Row(reference: "R1", title: "B", amount: -99m)));
        Key(Row(reference: "R1")).ShouldNotBe(Key(Row(reference: "R2")));
    }

    /// <summary>Only booked, outgoing, same-currency purchases are candidates (§3).</summary>
    [Fact]
    public void SkipReason_ClassifiesRows()
    {
        StatementCandidateRules.SkipReason(Row()).ShouldBeNull();
        StatementCandidateRules.SkipReason(Row(booked: false)).ShouldBe(StatementSkipReason.NotBooked);
        StatementCandidateRules.SkipReason(Row(amount: 10m)).ShouldBe(StatementSkipReason.Incoming);
        StatementCandidateRules.SkipReason(Row(foreign: true)).ShouldBe(StatementSkipReason.CurrencyMismatch);
        StatementCandidateRules.SkipReason(Row(type: "RĘCZNA SPŁATA KARTY KREDYT.")).ShouldBe(StatementSkipReason.OwnTransfer);
        StatementCandidateRules.SkipReason(Row(type: "EXCHANGE")).ShouldBe(StatementSkipReason.OwnTransfer);
        StatementCandidateRules.SkipReason(Row(type: "WYPŁATA W BANKOMACIE")).ShouldBe(StatementSkipReason.CashWithdrawal);
    }

    /// <summary>Counterparty and title are joined when both exist, and cut to the 80-character expense limit.</summary>
    [Fact]
    public void ExpenseDescription_JoinsAndTruncates()
    {
        StatementText.ExpenseDescription(Row(counterparty: "JAN", title: "CZYNSZ")).ShouldBe("JAN — CZYNSZ");
        StatementText.ExpenseDescription(Row(title: "SKLEP")).ShouldBe("SKLEP");
        StatementText.ExpenseDescription(Row(title: new string('a', 100))).Length.ShouldBe(80);
    }
}
