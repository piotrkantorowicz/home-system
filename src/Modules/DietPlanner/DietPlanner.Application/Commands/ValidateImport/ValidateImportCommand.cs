namespace DietPlanner.Application.Commands.ValidateImport;

using Shared.Abstractions.CQRS;

public sealed record ValidateImportCommand(ImportDto Import, string UserId) : ICommand<ValidationResultDto>;
