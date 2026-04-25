namespace DietPlanner.Application.Commands.ValidateImport;

using Shared.Abstractions.Cqrs;

public sealed record ValidateImportCommand(ImportDto Import, string UserId) : ICommand<ValidationResultDto>;
