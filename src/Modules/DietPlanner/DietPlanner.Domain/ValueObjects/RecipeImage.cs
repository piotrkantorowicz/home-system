namespace DietPlanner.Domain.ValueObjects;

using DietPlanner.Domain.Exceptions;

/// <summary>Public metadata for a normalized recipe photo; contains no storage keys.</summary>
/// <param name="Version">Opaque revision identifier.</param>
/// <param name="Width">Detail width.</param>
/// <param name="Height">Detail height.</param>
/// <param name="Size">Detail byte length.</param>
/// <param name="ThumbnailWidth">Thumbnail width.</param>
/// <param name="ThumbnailHeight">Thumbnail height.</param>
/// <param name="ThumbnailSize">Thumbnail byte length.</param>
public sealed record RecipeImage(Guid Version, int Width, int Height, int Size,
    int ThumbnailWidth, int ThumbnailHeight, int ThumbnailSize)
{
    /// <summary>Maximum detail width or height.</summary>
    public const int MaxDetailDimension = 1600;
    /// <summary>Maximum thumbnail width or height.</summary>
    public const int MaxThumbnailDimension = 320;
    /// <summary>Maximum detail byte length.</summary>
    public const int MaxDetailSize = 1024 * 1024;
    /// <summary>Maximum thumbnail byte length.</summary>
    public const int MaxThumbnailSize = 100 * 1024;
    /// <summary>Nonempty immutable revision.</summary>
    public Guid Version { get; } = Version != Guid.Empty ? Version : throw Invalid();
    /// <summary>Bounded detail width.</summary>
    public int Width { get; } = Bounded(Width, MaxDetailDimension);
    /// <summary>Bounded detail height.</summary>
    public int Height { get; } = Bounded(Height, MaxDetailDimension);
    /// <summary>Bounded detail byte length.</summary>
    public int Size { get; } = Bounded(Size, MaxDetailSize);
    /// <summary>Bounded thumbnail width.</summary>
    public int ThumbnailWidth { get; } = Bounded(ThumbnailWidth, MaxThumbnailDimension);
    /// <summary>Bounded thumbnail height.</summary>
    public int ThumbnailHeight { get; } = Bounded(ThumbnailHeight, MaxThumbnailDimension);
    /// <summary>Bounded thumbnail byte length.</summary>
    public int ThumbnailSize { get; } = Bounded(ThumbnailSize, MaxThumbnailSize);

    private static int Bounded(int value, int maximum) => value > 0 && value <= maximum ? value : throw Invalid();
    private static DietPlannerDomainException Invalid() => new("Normalized recipe photo metadata is invalid.");
}
