namespace DietPlanner.Application.Images;

/// <summary>Validates and normalizes a bounded photo upload.</summary>
public interface IRecipeImageProcessor
{
    /// <summary>Checks the decoded format, applies orientation and strips metadata.</summary>
    /// <param name="input">Caller-owned upload.</param>
    /// <param name="contentType">Declared media type.</param>
    /// <param name="fileName">Declared filename.</param>
    /// <param name="ct">Cancellation.</param>
    Task<ProcessedRecipeImage> ProcessAsync(Stream input, string contentType, string fileName, CancellationToken ct);
}
