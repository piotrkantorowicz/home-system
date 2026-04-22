namespace DietPlanner.Application.Commands.PurgeUserData;

using Shared.Abstractions.CQRS;

public sealed record PurgeUserDataCommand(string UserId) : ICommand;
