namespace DietPlanner.Application.Commands.SetShoppingCheck;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Ticks or unticks one shopping-list row for the whole household. Idempotent: repeating the same
/// state changes nothing. The row is identified by the date range of the list plus product and unit.
/// </summary>
/// <param name="PersonId">Person identifier of the caller.</param>
/// <param name="AuthSubject">Auth subject of the caller; used to resolve household membership.</param>
/// <param name="From">First day of the list's range, or <see langword="null"/> for no lower bound.</param>
/// <param name="To">Last day of the list's range, or <see langword="null"/> for no upper bound.</param>
/// <param name="ProductId">The product of the row.</param>
/// <param name="Unit">The unit of the row; the same product in another unit is a separate row.</param>
/// <param name="IsChecked"><see langword="true"/> to mark bought, <see langword="false"/> to clear it.</param>
public sealed record SetShoppingCheckCommand(
    Guid PersonId,
    string AuthSubject,
    DateOnly? From,
    DateOnly? To,
    Guid ProductId,
    string Unit,
    bool IsChecked) : ICommand;
