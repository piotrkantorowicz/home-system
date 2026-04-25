namespace DietPlanner.Application.Queries.GetProductById;

using DietPlanner.Application.Queries.SearchProducts;
using Shared.Abstractions.Cqrs;

public sealed record GetProductByIdQuery(Guid Id, string UserId) : IQuery<ProductDto?>;
