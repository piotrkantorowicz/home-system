namespace DietPlanner.Application.Commands.ExecuteImport;

using DietPlanner.Application.Commands.ValidateImport;
using Shared.Abstractions.Cqrs;

public sealed record ExecuteImportCommand(ImportDto Import, string UserId) : ICommand<ImportResultDto>;
