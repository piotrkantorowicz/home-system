namespace DietPlanner.Application.Commands.PurgeUserData;

using Shared.Abstractions.Cqrs;

public sealed record PurgeUserDataCommand(string UserId) : ICommand;
