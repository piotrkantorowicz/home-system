namespace DietPlanner.Application.Commands.BulkCompleteMealEntries;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Marks every still-planned meal of one person on one day as done; entries already done or modified are left alone.
/// </summary>
/// <param name="PersonId">Person identifier of the caller.</param>
/// <param name="Date">The calendar day.</param>
/// <param name="AuthSubject">Auth subject of the caller; resolves their household.</param>
/// <param name="ForPersonId">Whose meals to complete: the caller when <see langword="null"/>, otherwise a managed member the caller (Owner/Adult) looks after.</param>
public sealed record BulkCompleteMealEntriesCommand(
    Guid PersonId,
    DateOnly Date,
    string AuthSubject,
    Guid? ForPersonId = null) : ICommand<BulkCompleteResult>;

/// <summary>
/// Outcome of a bulk completion.
/// </summary>
/// <param name="Completed">How many entries changed from planned to done.</param>
public sealed record BulkCompleteResult(int Completed);
