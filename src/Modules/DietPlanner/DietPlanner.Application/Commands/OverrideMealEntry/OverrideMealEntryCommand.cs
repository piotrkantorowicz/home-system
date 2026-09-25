namespace DietPlanner.Application.Commands.OverrideMealEntry;

using Shared.Abstractions.Cqrs;

/// <summary>
/// A product actually eaten, supplied when overriding a meal.
/// </summary>
/// <param name="ProductId">The product eaten; must exist.</param>
/// <param name="Amount">Quantity; positive.</param>
/// <param name="Unit">Unit of the amount.</param>
public sealed record ActualProductInput(Guid ProductId, decimal Amount, string Unit);

/// <summary>
/// Records that a meal was eaten differently from the plan — a replacement recipe, individual products, or both — and marks the entry as modified.
/// </summary>
/// <param name="Id">Identifier of the entry; must belong to the caller.</param>
/// <param name="PersonId">Person identifier of the caller; the command only touches this user's data.</param>
/// <param name="ActualRecipeId">Recipe eaten instead of the planned one, or <see langword="null"/>.</param>
/// <param name="ActualProducts">Products eaten; may be empty when a recipe is given, but not both.</param>
/// <param name="AuthSubject">Auth subject of the caller; used to check ownership of the shared recipe/product rows referenced, which still key on it.</param>
public sealed record OverrideMealEntryCommand(
    Guid Id,
    Guid PersonId,
    Guid? ActualRecipeId,
    IReadOnlyList<ActualProductInput> ActualProducts, string AuthSubject) : ICommand;
