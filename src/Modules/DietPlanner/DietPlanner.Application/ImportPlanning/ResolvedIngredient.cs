namespace DietPlanner.Application.ImportPlanning;

using DietPlanner.Domain.ValueObjects;

/// <summary>
/// One ingredient line of a new imported recipe with its product reference resolved.
/// </summary>
/// <param name="ProductId">Existing or batch-created product.</param>
/// <param name="Amount">Quantity for the whole recipe.</param>
/// <param name="Unit">Unit of <paramref name="Amount"/>.</param>
internal sealed record ResolvedIngredient(ProductId ProductId, decimal Amount, string Unit);
