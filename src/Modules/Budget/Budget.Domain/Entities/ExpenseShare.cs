namespace Budget.Domain.Entities;

using Budget.Domain.ValueObjects;

/// <summary>One participant's exact share of a shared expense. Never recalculated from later rosters.</summary>
public sealed class ExpenseShare
{
    private ExpenseShare() { }

    internal ExpenseShare(PersonRef person, Money amount)
    {
        PersonId = person.PersonId;
        PersonDisplayName = person.DisplayName;
        Amount = amount.Amount;
    }

    /// <summary>The owning expense.</summary>
    public ExpenseId ExpenseId { get; private set; } = default!;

    /// <summary>Who owes the share.</summary>
    public Guid PersonId { get; private set; }

    /// <summary>Name snapshot taken when the share was stored.</summary>
    public string PersonDisplayName { get; private set; } = default!;

    /// <summary>Exact amount, two decimals.</summary>
    public decimal Amount { get; private set; }
}
