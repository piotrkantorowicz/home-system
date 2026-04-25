namespace DietPlanner.Application.Commands.DeleteProduct;

using Shared.Abstractions.Cqrs;

public sealed record DeleteProductCommand(Guid Id, string UserId) : ICommand;
