namespace Budget.UnitTests.Application.StatementImports;

using global::Budget.Application.StatementImports;
using global::Budget.Domain.Aggregates;

/// <summary>#673 — the merchant a category rule is learned for (§5.1).</summary>
public sealed class StatementMerchantKeyTests
{
    private static StatementTransaction Row(string counterparty, string title)
        => new(1, "00 1140", new(2026, 10, 20), new(2026, 10, 19), null, -10m, "PLN", false, counterparty, title, null, "PRZELEW", true);

    /// <summary>The counterparty wins; without one, the title up to its first slash; case and spacing are normalised.</summary>
    [Fact]
    public void MerchantKey_UsesCounterpartyElseTitleBeforeSlash()
    {
        StatementText.MerchantKey(Row("  Zakład  Wodociągów ", "Faktura 12/2026")).ShouldBe("ZAKŁAD WODOCIĄGÓW");
        StatementText.MerchantKey(Row("", "Kaufland 6663      /GLIWICE")).ShouldBe("KAUFLAND 6663");
        StatementText.MerchantKey(Row("", "/ONLY A CITY")).ShouldBeNull();
    }

    /// <summary>A key sent back on import normalises to itself and never exceeds the column.</summary>
    [Fact]
    public void MerchantKey_IsIdempotentAndBounded()
    {
        StatementText.MerchantKey("KAUFLAND 6663").ShouldBe("KAUFLAND 6663");
        StatementText.MerchantKey(new string('a', 150))!.Length.ShouldBe(CategoryRule.MaxMerchantKeyLength);
        StatementText.MerchantKey("   ").ShouldBeNull();
    }
}
