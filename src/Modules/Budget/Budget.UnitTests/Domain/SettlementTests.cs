namespace Budget.UnitTests.Domain;

using global::Budget.Domain.Aggregates;
using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>Invariants of the <c>Settlement</c> (recorded repayment) aggregate.</summary>
public sealed class SettlementTests
{
    private static readonly PersonRef Alex = new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Alex");
    private static readonly PersonRef Bea = new(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Bea");
    private static readonly DateOnly Day = new(2026, 10, 1);

    private static Settlement New(PersonRef? from = null, PersonRef? to = null, string? note = null)
        => Settlement.Create(
            SettlementId.New(), BudgetId.New(), from ?? Bea, to ?? Alex, Money.FromMinorUnits(2500), Day, note, Alex,
            Guid.CreateVersion7(), TestClock.UtcNow);

    /// <summary>A repayment keeps who paid whom, with name snapshots, the recorder and revision 1.</summary>
    [Fact]
    public void Create_StoresAttributionAndSnapshots()
    {
        var s = New(note: "  cash  ");

        (s.FromPersonId, s.FromDisplayName, s.ToPersonId, s.ToDisplayName).ShouldBe((Bea.PersonId, "Bea", Alex.PersonId, "Alex"));
        s.Amount.ShouldBe(25m);
        s.Note.ShouldBe("cash");
        s.AddedByPersonId.ShouldBe(Alex.PersonId);
        s.Revision.ShouldBe(1);
        s.IsVoided.ShouldBeFalse();
    }

    /// <summary>Sender and recipient cannot be the same person; an over-long note is rejected; a blank note is none.</summary>
    [Fact]
    public void Create_RejectsSamePersonAndLongNote()
    {
        Should.Throw<BudgetDomainException>(() => New(from: Alex, to: Alex));
        Should.Throw<BudgetDomainException>(() => New(note: new string('x', Settlement.MaxNoteLength + 1)));
        New(note: "   ").Note.ShouldBeNull();
    }

    /// <summary>Overpayment is the caller's business, not the aggregate's: any positive amount is recorded.</summary>
    [Fact]
    public void Create_AcceptsAnyPositiveAmount()
        => Settlement.Create(
            SettlementId.New(), BudgetId.New(), Bea, Alex, Money.FromMinorUnits(999_999_999_999), Day, null, Alex,
            Guid.CreateVersion7(), TestClock.UtcNow).Amount.ShouldBe(9999999999.99m);

    /// <summary>A retry matches only when every creation field is the same.</summary>
    [Fact]
    public void MatchesCreation_ComparesEveryField()
    {
        var s = New(note: "cash");
        var amount = Money.FromMinorUnits(2500);

        s.MatchesCreation(Bea.PersonId, Alex.PersonId, amount, Day, " cash ").ShouldBeTrue();
        s.MatchesCreation(Alex.PersonId, Bea.PersonId, amount, Day, "cash").ShouldBeFalse();
        s.MatchesCreation(Bea.PersonId, Alex.PersonId, Money.FromMinorUnits(2501), Day, "cash").ShouldBeFalse();
        s.MatchesCreation(Bea.PersonId, Alex.PersonId, amount, Day.AddDays(1), "cash").ShouldBeFalse();
        s.MatchesCreation(Bea.PersonId, Alex.PersonId, amount, Day, null).ShouldBeFalse();
    }

    /// <summary>Voiding records who/when/why, bumps the revision and keeps the financial fields.</summary>
    [Fact]
    public void Void_RecordsMetadata_AndKeepsFinancialFields()
    {
        var s = New();

        s.Void(1, "  entered twice ", Bea, TestClock.UtcNow);

        s.IsVoided.ShouldBeTrue();
        s.Revision.ShouldBe(2);
        s.VoidReason.ShouldBe("entered twice");
        s.VoidedByPersonId.ShouldBe(Bea.PersonId);
        s.Amount.ShouldBe(25m);
        (s.FromPersonId, s.ToPersonId).ShouldBe((Bea.PersonId, Alex.PersonId));
    }

    /// <summary>A stale revision conflicts and a blank reason is rejected; nothing changes either way.</summary>
    [Fact]
    public void Void_StaleRevisionOrBlankReason_Throws()
    {
        var s = New();

        Should.Throw<ConflictException>(() => s.Void(5, "x", Bea, TestClock.UtcNow));
        Should.Throw<BudgetDomainException>(() => s.Void(1, " ", Bea, TestClock.UtcNow));

        s.IsVoided.ShouldBeFalse();
        s.Revision.ShouldBe(1);
    }
}
