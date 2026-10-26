namespace Budget.UnitTests.Domain;

using global::Budget.Domain.Aggregates;
using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;

/// <summary>Invariants of the <c>CategoryRule</c> aggregate.</summary>
public sealed class CategoryRuleTests
{
    private static CategoryRule Rule()
        => CategoryRule.Create(CategoryRuleId.New(), BudgetId.New(), Expense.HouseholdImportScope, "KAUFLAND 6663", ExpenseCategory.Groceries, TestClock.UtcNow);

    /// <summary>A new choice replaces the category; the same choice leaves the rule untouched.</summary>
    [Fact]
    public void Learn_ChangedOrSameCategory_UpdatesOnlyOnChange()
    {
        var rule = Rule();
        var later = TestClock.UtcNow.AddDays(1);

        rule.Learn(ExpenseCategory.Groceries, later);
        rule.UpdatedAt.ShouldBe(TestClock.UtcNow);

        rule.Learn(ExpenseCategory.Leisure, later);
        (rule.Category, rule.UpdatedAt).ShouldBe((ExpenseCategory.Leisure, later));
    }

    /// <summary>A blank or oversized merchant key is rejected.</summary>
    [Fact]
    public void Create_BlankOrLongMerchantKey_Throws()
    {
        Should.Throw<BudgetDomainException>(() => CategoryRule.Create(
            CategoryRuleId.New(), BudgetId.New(), Expense.HouseholdImportScope, " ", ExpenseCategory.Other, TestClock.UtcNow));
        Should.Throw<BudgetDomainException>(() => CategoryRule.Create(
            CategoryRuleId.New(), BudgetId.New(), Expense.HouseholdImportScope, new string('a', 101), ExpenseCategory.Other, TestClock.UtcNow));
    }
}
