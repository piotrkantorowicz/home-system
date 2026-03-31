namespace DietPlanner.Application.Queries.GetProductById;

using DietPlanner.Application.Queries.SearchProducts;
using Shared.Abstractions.CQRS;

public sealed record GetProductByIdQuery(Guid Id, string UserId) : IQuery<ProductDto?>;
