namespace DietPlanner.Application.Commands.RemoveRecipeImage;

using Shared.Abstractions.Cqrs;

/// <summary>Revokes photo reads immediately; object deletion is durable and asynchronous.</summary>
/// <param name="Id">Recipe.</param>
/// <param name="UserId">Caller.</param>
public sealed record RemoveRecipeImageCommand(Guid Id, string UserId) : ICommand;
