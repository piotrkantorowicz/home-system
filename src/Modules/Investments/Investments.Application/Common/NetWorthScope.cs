namespace Investments.Application.Common;

using Investments.Domain.Aggregates;
using Shared.Abstractions.Cqrs;

/// <summary>Which wallets a net-worth figure covers (design §4).</summary>
internal static class NetWorthScope
{
    public const string Household = "household";
    public const string Mine = "mine";

    /// <summary>Normalises the query value; blank means household.</summary>
    public static string Parse(string? scope, string queryName)
    {
        var value = string.IsNullOrWhiteSpace(scope) ? Household : scope.Trim().ToLowerInvariant();
        return value is Household or Mine
            ? value
            : throw new CommandValidationException(queryName, [new ValidationError("Scope", "Scope must be household or mine.")]);
    }

    /// <summary>
    /// Household: every wallet the caller may see (shared, own, managed members'). Mine: the caller's
    /// own. Archived wallets never count.
    /// </summary>
    public static IQueryable<Wallet> InScope(this IQueryable<Wallet> wallets, InvestmentsCaller caller, string scope)
    {
        var personId = caller.PersonId;
        var visible = wallets.VisibleTo(caller).Where(w => !w.IsArchived);
        return scope == Mine ? visible.Where(w => w.OwnerPersonId == personId) : visible;
    }
}
