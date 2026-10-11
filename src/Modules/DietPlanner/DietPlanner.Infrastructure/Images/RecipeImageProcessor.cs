namespace DietPlanner.Infrastructure.Images;

using System.Buffers.Binary;
using DietPlanner.Application.Images;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using SkiaSharp;

internal sealed class RecipeImageProcessor : IRecipeImageProcessor
{
    private const int MaxInputBytes = 10 * 1024 * 1024;
    private const long MaxPixels = 25_000_000;
    // PNG may require a full-resolution decode; serialize native work to bound concurrent pixel allocations.
    private static readonly SemaphoreSlim DecodeGate = new(1, 1);

    public async Task<ProcessedRecipeImage> ProcessAsync(Stream input, string contentType, string fileName, CancellationToken ct)
    {
        await DecodeGate.WaitAsync(ct);
        try
        {
            return await ProcessCoreAsync(input, contentType, fileName, ct);
        }
        finally
        {
            DecodeGate.Release();
        }
    }

    private static async Task<ProcessedRecipeImage> ProcessCoreAsync(Stream input, string contentType, string fileName, CancellationToken ct)
    {
        using var bytes = new MemoryStream();
        byte[] buffer = new byte[81920];
        int count;
        while ((count = await input.ReadAsync(buffer, ct)) > 0)
        {
            if (bytes.Length + count > MaxInputBytes)
                throw Invalid("Photos must be at most 10 MiB.");
            await bytes.WriteAsync(buffer.AsMemory(0, count), ct);
        }
        ct.ThrowIfCancellationRequested();
        bytes.Position = 0;
        using var stream = new SKManagedStream(bytes, false);
        using SKCodec codec = SKCodec.Create(stream) ?? throw Invalid("Photo data is malformed or unsupported.");
        string actual = codec.EncodedFormat switch
        {
            SKEncodedImageFormat.Jpeg => "image/jpeg",
            SKEncodedImageFormat.Png => "image/png",
            SKEncodedImageFormat.Webp => "image/webp",
            _ => throw Invalid("Use JPEG, PNG or static WebP."),
        };
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        bool matches = actual switch
        {
            "image/jpeg" => extension is ".jpg" or ".jpeg",
            "image/png" => extension == ".png",
            _ => extension == ".webp",
        };
        if (contentType != actual || !matches)
            throw Invalid("Photo type and filename must match the decoded format.");
        ReadOnlyMemory<byte> source = bytes.GetBuffer().AsMemory(0, (int)bytes.Length);
        if (codec.FrameCount > 1 || actual == "image/png" && HasAnimationChunk(source.Span)
            || actual == "image/webp" && HasWebpAnimation(source.Span))
            throw Invalid("Animated photos are unsupported.");
        SKImageInfo info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 || (long)info.Width * info.Height > MaxPixels)
            throw Invalid("Photos must be at most 25 megapixels.");
        float scale = Math.Min(1f, (float)RecipeImage.MaxDetailDimension / Math.Max(info.Width, info.Height));
        SKSizeI dimensions = codec.GetScaledDimensions(scale);
        // JPEG rounds to supported fractions; retain enough pixels for the detail output without upscaling.
        while (Math.Max(dimensions.Width, dimensions.Height) < Math.Min(RecipeImage.MaxDetailDimension, Math.Max(info.Width, info.Height)) && scale < 1f)
        {
            scale = Math.Min(1f, scale + 0.125f);
            dimensions = codec.GetScaledDimensions(scale);
        }
        using var decoded = new SKBitmap(new SKImageInfo(dimensions.Width, dimensions.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (codec.GetPixels(decoded.Info, decoded.GetPixels()) != SKCodecResult.Success)
            throw Invalid("Photo data is incomplete or malformed.");
        ct.ThrowIfCancellationRequested();
        using SKBitmap? oriented = codec.EncodedOrigin == SKEncodedOrigin.TopLeft ? null : Orient(decoded, codec.EncodedOrigin);
        using SKImage pixels = SKImage.FromBitmap(oriented ?? decoded);
        (byte[] detail, int width, int height) = Encode(pixels, RecipeImage.MaxDetailDimension, RecipeImage.MaxDetailSize, ct);
        (byte[] thumbnail, int thumbnailWidth, int thumbnailHeight) = Encode(pixels, RecipeImage.MaxThumbnailDimension, RecipeImage.MaxThumbnailSize, ct);
        return new ProcessedRecipeImage(detail, thumbnail, new RecipeImageDto(Guid.CreateVersion7(), width, height,
            detail.Length, thumbnailWidth, thumbnailHeight, thumbnail.Length));
    }

    private static bool HasAnimationChunk(ReadOnlySpan<byte> png)
    {
        int offset = 8;
        while (offset + 12 <= png.Length)
        {
            uint length = BinaryPrimitives.ReadUInt32BigEndian(png[offset..]);
            if (png.Slice(offset + 4, 4).SequenceEqual("acTL"u8))
                return true;
            if (length > (uint)(png.Length - offset - 12))
                throw Invalid("Photo data is malformed.");
            offset += (int)length + 12;
        }
        return false;
    }

    private static bool HasWebpAnimation(ReadOnlySpan<byte> webp)
    {
        int offset = 12;
        while (offset + 8 <= webp.Length)
        {
            ReadOnlySpan<byte> type = webp.Slice(offset, 4);
            uint length = BinaryPrimitives.ReadUInt32LittleEndian(webp[(offset + 4)..]);
            if (length > (uint)(webp.Length - offset - 8))
                throw Invalid("Photo data is malformed.");
            if (type.SequenceEqual("ANIM"u8) || type.SequenceEqual("ANMF"u8)
                || type.SequenceEqual("VP8X"u8) && length > 0 && (webp[offset + 8] & 2) != 0)
                return true;
            offset += (int)length + 8 + (int)(length & 1);
        }
        return false;
    }

    private static SKBitmap Orient(SKBitmap source, SKEncodedOrigin origin)
    {
        bool swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var output = new SKBitmap(swap ? source.Height : source.Width, swap ? source.Width : source.Height);
        using var canvas = new SKCanvas(output);
        canvas.Clear(SKColors.White);
        switch (origin)
        {
            case SKEncodedOrigin.TopRight: canvas.Translate(source.Width, 0); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.BottomRight: canvas.Translate(source.Width, source.Height); canvas.RotateDegrees(180); break;
            case SKEncodedOrigin.BottomLeft: canvas.Translate(0, source.Height); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.RightTop: canvas.Translate(source.Height, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom: canvas.Translate(source.Height, source.Width); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.LeftBottom: canvas.Translate(0, source.Width); canvas.RotateDegrees(270); break;
        }
        canvas.DrawBitmap(source, 0, 0);
        return output;
    }

    private static (byte[] Bytes, int Width, int Height) Encode(SKImage source, int longestSide, int limit, CancellationToken ct)
    {
        double scale = Math.Min(1d, (double)longestSide / Math.Max(source.Width, source.Height));
        int width = Math.Max(1, (int)Math.Round(source.Width * scale));
        int height = Math.Max(1, (int)Math.Round(source.Height * scale));
        using var resized = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(resized))
        {
            canvas.Clear(SKColors.White);
            canvas.DrawImage(source, new SKRect(0, 0, width, height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        }
        using SKImage normalized = SKImage.FromBitmap(resized);
        for (int quality = 85; quality >= 35; quality -= 10)
        {
            ct.ThrowIfCancellationRequested();
            using SKData encoded = normalized.Encode(SKEncodedImageFormat.Jpeg, quality);
            if (encoded.Size <= limit)
                return (encoded.ToArray(), width, height);
        }
        throw Invalid("Photo cannot meet normalized size limits.");
    }

    private static DietPlannerDomainException Invalid(string message) => new(message);
}
