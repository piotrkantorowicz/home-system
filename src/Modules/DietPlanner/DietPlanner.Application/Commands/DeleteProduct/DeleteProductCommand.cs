namespace DietPlanner.Application.Commands.DeleteProduct;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Soft-deletes a product so it disappears from lists while past meals keep resolving it.
/// </summary>
/// <param name="Id">Identifier of the product; must be owned by the caller.</param>
/// <param name="UserId">Auth subject of the caller; the command only touches this user's data.</param>
public sealed record DeleteProductCommand(Guid Id, string UserId) : ICommand;
