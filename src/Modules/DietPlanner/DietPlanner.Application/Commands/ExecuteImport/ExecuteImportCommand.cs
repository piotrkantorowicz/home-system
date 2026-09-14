namespace DietPlanner.Application.Commands.ExecuteImport;

using DietPlanner.Application.Commands.ValidateImport;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Applies an import file for the user: creates or reuses products and recipes by name and plans the meals. Fails with a validation error when the file has issues of severity <c>error</c>.
/// </summary>
/// <param name="Import">The parsed import file.</param>
/// <param name="UserId">Auth subject of the importing user.</param>
public sealed record ExecuteImportCommand(ImportDto Import, string UserId) : ICommand<ImportResultDto>;
