namespace Budget.Application.Common;

using System.Linq.Expressions;
using Budget.Application.Queries.GetAccount;
using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;

/// <summary>The one place envelope visibility is applied to queries, before filtering, counting and lookup.</summary>
internal static class AccountVisibilityFilter
{
    public static IQueryable<BudgetAccount> VisibleTo(this IQueryable<BudgetAccount> accounts, BudgetCaller caller)
    {
        var owners = caller.PersonalOwnerIds;
        var seesHousehold = caller.IsAdult;
        return accounts.Where(a =>
            (seesHousehold && a.Visibility == AccountVisibility.Household)
            || (a.Visibility == AccountVisibility.Personal && owners.Contains(a.OwnerPersonId)));
    }

    public static readonly Expression<Func<BudgetAccount, AccountDto>> ToDto = a => new AccountDto(
        a.Id.Value, a.Name, a.Visibility.ToString(), a.OwnerPersonId, a.IsArchived, a.Revision, a.CreatedAt);

    public static AccountDto ToAccountDto(this BudgetAccount a) => new(
        a.Id.Value, a.Name, a.Visibility.ToString(), a.OwnerPersonId, a.IsArchived, a.Revision, a.CreatedAt);
}
