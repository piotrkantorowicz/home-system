namespace DietPlanner.Application.Queries.GetProductById;

using DietPlanner.Application.Queries.SearchProducts;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads one product visible to the caller; <see langword="null"/> when it does not exist or is soft-deleted.
/// </summary>
/// <param name="Id">Identifier of the product.</param>
/// <param name="UserId">Auth subject of the caller.</param>
public sealed record GetProductByIdQuery(Guid Id, string UserId) : IQuery<ProductDto?>;
