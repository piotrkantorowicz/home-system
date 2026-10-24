namespace DietPlanner.Application.ImportPlanning;

using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Domain.ValueObjects;

/// <summary>
/// One product of an import after resolution: either an existing catalog product to reuse or a new one
/// whose id is already assigned so recipe ingredients can reference it before it is written.
/// </summary>
/// <param name="Id">Existing product id, or the id the new product will get.</param>
/// <param name="Definition">The import definition that wins for this name.</param>
/// <param name="IsNew">True when executing the import creates the product.</param>
internal sealed record ResolvedProduct(ProductId Id, ImportProductDto Definition, bool IsNew);
