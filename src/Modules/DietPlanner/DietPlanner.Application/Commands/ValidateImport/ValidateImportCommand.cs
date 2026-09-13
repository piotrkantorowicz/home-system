namespace DietPlanner.Application.Commands.ValidateImport;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Dry-runs an import file for the user without writing anything and returns every issue and the resulting plan.
/// </summary>
/// <param name="Import">The parsed import file.</param>
/// <param name="UserId">Auth subject of the importing user.</param>
public sealed record ValidateImportCommand(ImportDto Import, string UserId) : ICommand<ValidationResultDto>;
