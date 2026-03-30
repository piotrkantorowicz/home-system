namespace DietPlanner.Application.Commands.DeleteProduct;

using Shared.Abstractions.CQRS;

public sealed record DeleteProductCommand(Guid Id, string UserId) : ICommand;
