namespace DietPlanner.Application.Commands.ExecuteImport;

using DietPlanner.Application.Commands.ValidateImport;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Applies an import file for the user: creates or reuses products and recipes by name and plans the meals,
/// all in one commit. Fails with a validation error, writing nothing, when the file has issues of severity <c>error</c>.
/// </summary>
/// <param name="Import">The parsed import file.</param>
/// <param name="UserId">Auth subject of the importing user.</param>
/// <param name="PersonId">Person identifier of the importing user.</param>
/// <param name="Strict">Same meaning as <see cref="ValidateImportCommand.Strict"/>; strict execution never creates a schedule or slot.</param>
/// <param name="AppendToOccupiedSlots">Same meaning as <see cref="ValidateImportCommand.AppendToOccupiedSlots"/>.</param>
public sealed record ExecuteImportCommand(
    ImportDto Import,
    string UserId,
    Guid PersonId,
    bool Strict = false,
    bool AppendToOccupiedSlots = false) : ICommand<ImportResultDto>;
