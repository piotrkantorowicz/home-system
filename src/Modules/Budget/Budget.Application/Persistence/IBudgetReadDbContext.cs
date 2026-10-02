namespace Budget.Application.Persistence;

using Budget.Domain.Aggregates;
using Budget.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using BudgetAggregate = Budget.Domain.Aggregates.Budget;

/// <summary>
/// Read-side surface over the module's <c>DbContext</c>. Query handlers project with
/// <c>AsNoTracking()</c> + <c>Select()</c> — they never load aggregates.
/// </summary>
public interface IBudgetReadDbContext
{
    /// <summary>Every household's budget.</summary>
    DbSet<BudgetAggregate> Budgets { get; }

    /// <summary>Every envelope, in every budget.</summary>
    DbSet<BudgetAccount> BudgetAccounts { get; }

    /// <summary>Every expense, in every budget.</summary>
    DbSet<Expense> Expenses { get; }

    /// <summary>Every expense revision, in every budget.</summary>
    DbSet<ExpenseRevision> ExpenseRevisions { get; }

    /// <summary>Every monthly limit, in every budget.</summary>
    DbSet<MonthlyLimit> MonthlyLimits { get; }
}
