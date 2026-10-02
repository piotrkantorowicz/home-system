namespace Budget.Domain.ValueObjects;

/// <summary>
/// Exact equal split in minor units. Remainder cents go one each to the first participants in
/// ascending <c>PersonId</c> order, so 100.00 over three people is 33.34 / 33.33 / 33.33. The
/// bias toward low IDs is accepted for v1.
/// </summary>
public static class EqualSplit
{
    /// <summary>Splits <paramref name="total"/> among distinct participants.</summary>
    /// <param name="total">The expense amount.</param>
    /// <param name="participants">Distinct participants; at least one.</param>
    /// <returns>One share per participant, ordered by <c>PersonId</c>; they sum to exactly <paramref name="total"/>.</returns>
    public static IReadOnlyList<(PersonRef Person, Money Amount)> Compute(Money total, IReadOnlyCollection<PersonRef> participants)
    {
        var ordered = participants.OrderBy(p => p.PersonId).ToList();
        var each = total.MinorUnits / ordered.Count;
        var remainder = (int)(total.MinorUnits % ordered.Count);

        return [.. ordered.Select((p, i) => (p, Money.FromMinorUnits(each + (i < remainder ? 1 : 0))))];
    }
}
