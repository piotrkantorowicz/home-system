namespace DietPlanner.Application.Commands.PreviewImport;

/// <summary>
/// A product or recipe of an import file.
/// </summary>
/// <param name="Name">Name as defined in the file.</param>
/// <param name="IsNew">True when the import creates it; false when an existing item is reused unchanged.</param>
/// <param name="ExistingId">Id of the reused item, when reused.</param>
public sealed record PreviewCatalogItemDto(string Name, bool IsNew, Guid? ExistingId);
