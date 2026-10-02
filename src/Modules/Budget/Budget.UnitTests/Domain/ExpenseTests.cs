namespace Budget.UnitTests.Domain;

using global::Budget.Domain.Aggregates;
using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>Invariants of the <c>Expense</c> aggregate.</summary>
public sealed class ExpenseTests
{
    private static readonly PersonRef Alex = new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Alex");
    private static readonly PersonRef Bea = new(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Bea");
    private static readonly PersonRef Cy = new(Guid.Parse("00000000-0000-0000-0000-000000000003"), "Cy");

    private static BudgetAccount Account()
        => BudgetAccount.Create(BudgetAccountId.New(), BudgetId.New(), "Everyday", AccountVisibility.Household, null, TestClock.UtcNow);

    private static Expense Create(
        BudgetAccount? account = null, FundingSource funding = FundingSource.Individual, PersonRef? paidBy = null,
        IReadOnlyCollection<PersonRef>? participants = null, string amount = "100.00")
    {
        return Expense.Create(
            ExpenseId.New(), account ?? Account(), Money.FromDecimal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)), ExpenseCategory.Groceries, new DateOnly(2026, 10, 1), funding,
            funding == FundingSource.Individual ? paidBy ?? Bea : paidBy, Alex, participants ?? [], Guid.CreateVersion7(), TestClock.UtcNow);
    }

    /// <summary>Payer and recorder are separate; shares are stored exactly and revision 1 snapshots them.</summary>
    [Fact]
    public void Create_Shared_StoresSharesAndCreationRevision()
    {
        var expense = Create(participants: [Cy, Alex, Bea]);

        expense.AddedByPersonId.ShouldBe(Alex.PersonId);
        expense.PaidByPersonId.ShouldBe(Bea.PersonId);
        expense.Shares.Select(s => s.Amount).ShouldBe([33.34m, 33.33m, 33.33m]);
        expense.Revision.ShouldBe(1);
        var revision = expense.Revisions.Single();
        revision.RevisionNumber.ShouldBe(1);
        revision.Request.ParticipantIds.ShouldBe([Alex.PersonId, Bea.PersonId, Cy.PersonId]);
        revision.Snapshot.Shares.Select(s => (s.PersonDisplayName, s.Amount)).ShouldBe([("Alex", 33.34m), ("Bea", 33.33m), ("Cy", 33.33m)]);
        revision.Snapshot.AddedByDisplayName.ShouldBe("Alex");
    }

    /// <summary>Household funds credit nobody and are not split.</summary>
    [Fact]
    public void Create_HouseholdFunds_HasNoPayerOrShares()
    {
        var expense = Create(funding: FundingSource.HouseholdFunds);

        expense.PaidByPersonId.ShouldBeNull();
        expense.Shares.ShouldBeEmpty();
    }

    /// <summary>Household funds with a payer or participants are rejected.</summary>
    [Fact]
    public void Create_HouseholdFundsWithPayerOrParticipants_Throws()
    {
        var five = Money.FromMinorUnits(500);
        Should.Throw<BudgetDomainException>(() => Expense.Create(
            ExpenseId.New(), Account(), five, ExpenseCategory.Other, new DateOnly(2026, 10, 1), FundingSource.HouseholdFunds,
            Bea, Alex, [], Guid.CreateVersion7(), TestClock.UtcNow));
        Should.Throw<BudgetDomainException>(() => Expense.Create(
            ExpenseId.New(), Account(), five, ExpenseCategory.Other, new DateOnly(2026, 10, 1), FundingSource.HouseholdFunds,
            null, Alex, [Bea], Guid.CreateVersion7(), TestClock.UtcNow));
    }

    /// <summary>Individual funding needs a payer; duplicate participants are rejected.</summary>
    [Fact]
    public void Create_MissingPayerOrDuplicateParticipant_Throws()
    {
        var five = Money.FromMinorUnits(500);
        Should.Throw<BudgetDomainException>(() => Expense.Create(
            ExpenseId.New(), Account(), five, ExpenseCategory.Other, new DateOnly(2026, 10, 1), FundingSource.Individual,
            null, Alex, [], Guid.CreateVersion7(), TestClock.UtcNow));
        Should.Throw<BudgetDomainException>(() => Create(participants: [Alex, Alex]));
    }

    /// <summary>An archived envelope cannot receive entries.</summary>
    [Fact]
    public void Create_InArchivedEnvelope_Throws()
    {
        var account = Account();
        account.Archive(1);

        Should.Throw<BudgetDomainException>(() => Create(account));
    }

    /// <summary>Request matching ignores participant order only because the request is normalised, and detects any changed field.</summary>
    [Fact]
    public void Request_Matches_DetectsChanges()
    {
        var a = Create(participants: [Alex, Bea]).Revisions.Single().Request;

        a.Matches(a with { }).ShouldBeTrue();
        a.Matches(a with { Amount = "100.01" }).ShouldBeFalse();
        a.Matches(a with { ParticipantIds = [Alex.PersonId] }).ShouldBeFalse();
        a.Matches(a with { Category = ExpenseCategory.Other }).ShouldBeFalse();
    }

    private static ExpenseRequest UpdateRequest(Expense e, int expected, string amount) => new(
        "Update", e.BudgetAccountId.Value, amount, ExpenseCategory.Groceries, new DateOnly(2026, 10, 1), FundingSource.Individual,
        Bea.PersonId, [], e.Id.Value, expected, "fix");

    private static void Update(Expense e, int expected, string amount, params PersonRef[] participants)
        => e.Update(
            expected, "fix", Money.FromDecimal(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)), ExpenseCategory.Groceries,
            new DateOnly(2026, 10, 1), FundingSource.Individual, Bea, participants, Cy, Guid.CreateVersion7(),
            UpdateRequest(e, expected, amount), TestClock.UtcNow);

    /// <summary>An update appends revision 2 with the full new state; the recorder and the first revision are untouched.</summary>
    [Fact]
    public void Update_AppendsRevisionAndKeepsRecorder()
    {
        var expense = Create(participants: [Alex, Bea]);

        Update(expense, 1, "60.00", Alex, Bea);

        expense.Revision.ShouldBe(2);
        expense.AddedByPersonId.ShouldBe(Alex.PersonId);
        expense.Shares.Select(s => s.Amount).ShouldBe([30m, 30m]);
        expense.Revisions.Select(r => r.RevisionNumber).ShouldBe([1, 2]);
        expense.Revisions.First().Snapshot.Amount.ShouldBe(100m);
        expense.Revisions.Last().Snapshot.Amount.ShouldBe(60m);
        expense.Revisions.Last().ActorPersonId.ShouldBe(Cy.PersonId);
        expense.Revisions.Last().Reason.ShouldBe("fix");
    }

    /// <summary>Unchanged amount and participants keep the exact stored shares even when the equal split would differ.</summary>
    [Fact]
    public void Update_UnchangedAmountAndParticipants_KeepsStoredShares()
    {
        var expense = Create(participants: [Alex, Bea, Cy]);

        Update(expense, 1, "100.00", Alex, Bea, Cy);

        expense.Shares.Select(s => s.Amount).ShouldBe([33.34m, 33.33m, 33.33m]);
        expense.Revision.ShouldBe(2);
    }

    /// <summary>Changing participants recomputes shares for exactly that set.</summary>
    [Fact]
    public void Update_ChangedParticipants_RecomputesShares()
    {
        var expense = Create(participants: [Alex, Bea, Cy]);

        Update(expense, 1, "100.00", Alex, Cy);

        expense.Shares.Select(s => (s.PersonId, s.Amount)).OrderBy(x => x.PersonId)
            .ShouldBe([(Alex.PersonId, 50m), (Cy.PersonId, 50m)]);
    }

    /// <summary>A stale revision conflicts and changes nothing; a blank reason is rejected.</summary>
    [Fact]
    public void Update_StaleRevisionOrBlankReason_Throws()
    {
        var expense = Create(participants: [Alex]);

        Should.Throw<ConflictException>(() => Update(expense, 7, "5.00", Alex));
        Should.Throw<BudgetDomainException>(() => expense.Update(
            1, " ", Money.FromMinorUnits(500), ExpenseCategory.Other, new DateOnly(2026, 10, 1), FundingSource.Individual,
            Bea, [Alex], Cy, Guid.CreateVersion7(), UpdateRequest(expense, 1, "5.00"), TestClock.UtcNow));

        expense.Revision.ShouldBe(1);
        expense.Amount.ShouldBe(100m);
    }

    /// <summary>Void keeps the financial fields, appends a revision, and blocks further changes.</summary>
    [Fact]
    public void Void_AppendsRevisionAndBlocksEdits()
    {
        var expense = Create(participants: [Alex, Bea]);
        var request = ExpenseRequest.ForVoid(expense.BudgetAccountId.Value, expense.Id.Value, 1, "oops");

        expense.Void(1, "oops", Cy, Guid.CreateVersion7(), request, TestClock.UtcNow);

        expense.IsVoided.ShouldBeTrue();
        expense.Revision.ShouldBe(2);
        expense.Amount.ShouldBe(100m);
        expense.Revisions.Last().Snapshot.IsVoided.ShouldBeTrue();
        expense.Revisions.Last().Snapshot.Shares.Count.ShouldBe(2);
        Should.Throw<BudgetDomainException>(() => Update(expense, 2, "5.00", Alex));
        Should.Throw<BudgetDomainException>(() => expense.Void(2, "again", Cy, Guid.CreateVersion7(), request, TestClock.UtcNow));
    }
}

