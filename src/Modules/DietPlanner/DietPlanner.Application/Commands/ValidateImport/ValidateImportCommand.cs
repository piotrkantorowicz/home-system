namespace DietPlanner.Application.Commands.ValidateImport;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Dry-runs an import file for the user without writing anything and returns every issue and the resulting plan.
/// </summary>
/// <param name="Import">The parsed import file.</param>
/// <param name="UserId">Auth subject of the importing user.</param>
/// <param name="PersonId">Person identifier of the importing user; their schedule and diary are checked.</param>
/// <param name="Strict">True rejects every fallback (missing schedule, unknown meal type, ambiguous name, unsupported unit, occupied slot) and bounds the file to one month; false keeps the forgiving SPA behaviour and reports those as warnings.</param>
/// <param name="AppendToOccupiedSlots">In strict mode, true allows adding meals next to already planned ones instead of rejecting them.</param>
public sealed record ValidateImportCommand(
    ImportDto Import,
    string UserId,
    Guid PersonId,
    bool Strict = false,
    bool AppendToOccupiedSlots = false) : ICommand<ValidationResultDto>;
