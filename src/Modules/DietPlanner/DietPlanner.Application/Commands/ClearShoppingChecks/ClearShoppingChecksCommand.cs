namespace DietPlanner.Application.Commands.ClearShoppingChecks;

using Shared.Abstractions.Cqrs;

/// <summary>Unticks every row of one shopping-list range for the household. Other ranges are untouched.</summary>
/// <param name="PersonId">Person identifier of the caller.</param>
/// <param name="AuthSubject">Auth subject of the caller; used to resolve household membership.</param>
/// <param name="From">First day of the list's range, or <see langword="null"/> for no lower bound.</param>
/// <param name="To">Last day of the list's range, or <see langword="null"/> for no upper bound.</param>
public sealed record ClearShoppingChecksCommand(Guid PersonId, string AuthSubject, DateOnly? From, DateOnly? To) : ICommand;
