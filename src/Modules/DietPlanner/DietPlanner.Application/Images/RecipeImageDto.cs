namespace DietPlanner.Application.Images;

using DietPlanner.Domain.ValueObjects;

/// <summary>Normalized JPEG metadata; version changes on replacement.</summary>
/// <param name="Version">Revision.</param>
/// <param name="Width">Detail width.</param>
/// <param name="Height">Detail height.</param>
/// <param name="Size">Detail bytes.</param>
/// <param name="ThumbnailWidth">Thumbnail width.</param>
/// <param name="ThumbnailHeight">Thumbnail height.</param>
/// <param name="ThumbnailSize">Thumbnail bytes.</param>
/// <param name="ContentType">Normalized media type.</param>
public sealed record RecipeImageDto(Guid Version, int Width, int Height, int Size,
    int ThumbnailWidth, int ThumbnailHeight, int ThumbnailSize, string ContentType = "image/jpeg")
{
    internal static RecipeImageDto? From(RecipeImage? image)
        => image is null ? null : new(image.Version, image.Width, image.Height, image.Size,
            image.ThumbnailWidth, image.ThumbnailHeight, image.ThumbnailSize);
}
