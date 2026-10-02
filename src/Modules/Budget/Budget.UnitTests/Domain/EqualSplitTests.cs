namespace Budget.UnitTests.Domain;

using global::Budget.Domain.ValueObjects;

/// <summary>Exact equal splitting with deterministic remainder allocation.</summary>
public sealed class EqualSplitTests
{
    private static PersonRef Person(int n) => new(new Guid(n, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0]), $"P{n}");

    /// <summary>100.00 among three, in ascending ID order, is 33.34 / 33.33 / 33.33 regardless of input order.</summary>
    [Fact]
    public void HundredAmongThree_AllocatesRemainderToLowestIds()
    {
        var shares = EqualSplit.Compute(Money.FromMinorUnits(10000), [Person(3), Person(1), Person(2)]);

        shares.Select(s => s.Person.DisplayName).ShouldBe(["P1", "P2", "P3"]);
        shares.Select(s => s.Amount.ToString()).ShouldBe(["33.34", "33.33", "33.33"]);
    }

    /// <summary>Shares always sum to the total; tiny amounts give zero-cent shares but one positive.</summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 3)]
    [InlineData(2, 5)]
    [InlineData(10000, 3)]
    [InlineData(10001, 7)]
    [InlineData(999_999_999_999, 11)]
    public void Shares_AreConserved(long minor, int people)
    {
        var shares = EqualSplit.Compute(Money.FromMinorUnits(minor), [.. Enumerable.Range(1, people).Select(Person)]);

        shares.Sum(s => s.Amount.MinorUnits).ShouldBe(minor);
        shares.ShouldContain(s => s.Amount.MinorUnits > 0);
        (shares.Max(s => s.Amount.MinorUnits) - shares.Min(s => s.Amount.MinorUnits)).ShouldBeLessThanOrEqualTo(1);
    }
}
