namespace DietPlanner.Application.Images;

/// <summary>Bounded normalized JPEG bytes, never persisted in the database.</summary>
/// <param name="Detail">Detail bytes.</param>
/// <param name="Thumbnail">Thumbnail bytes.</param>
/// <param name="Metadata">Public metadata.</param>
public sealed record ProcessedRecipeImage(byte[] Detail, byte[] Thumbnail, RecipeImageDto Metadata);
