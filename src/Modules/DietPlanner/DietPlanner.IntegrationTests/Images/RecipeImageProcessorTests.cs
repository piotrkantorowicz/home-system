namespace DietPlanner.IntegrationTests.Images;

using DietPlanner.Application.Images;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Infrastructure.Images;
using SkiaSharp;

/// <summary>Real native decoder boundary checks.</summary>
public sealed class RecipeImageProcessorTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>All supported inputs normalize to bounded JPEG outputs.</summary>
    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, "image/jpeg", "photo.jpg")]
    [InlineData(SKEncodedImageFormat.Png, "image/png", "photo.png")]
    [InlineData(SKEncodedImageFormat.Webp, "image/webp", "photo.webp")]
    public async Task Process_SupportedFormat_ReturnsBoundedMetadataFreeJpeg(SKEncodedImageFormat format, string type, string name)
    {
        using var input = new MemoryStream(Photo(format, 2000, 1000));
        ProcessedRecipeImage result = await new RecipeImageProcessor().ProcessAsync(input, type, name, Ct);
        result.Metadata.ContentType.ShouldBe("image/jpeg");
        result.Metadata.Width.ShouldBe(1600);
        result.Metadata.Height.ShouldBe(800);
        result.Metadata.ThumbnailWidth.ShouldBe(320);
        result.Detail.Length.ShouldBeLessThanOrEqualTo(1024 * 1024);
        result.Thumbnail.Length.ShouldBeLessThanOrEqualTo(100 * 1024);
        using SKBitmap detail = SKBitmap.Decode(result.Detail);
        detail.Width.ShouldBe(result.Metadata.Width);
    }

    /// <summary>Declared format and filename must match content.</summary>
    [Theory]
    [InlineData("image/png", "photo.jpg")]
    [InlineData("image/jpeg", "photo.png")]
    [InlineData("image/svg+xml", "photo.jpg")]
    public async Task Process_MisdeclaredFormat_Rejects(string type, string name)
    {
        using var input = new MemoryStream(Photo(SKEncodedImageFormat.Jpeg));
        await Should.ThrowAsync<DietPlannerDomainException>(() => new RecipeImageProcessor().ProcessAsync(input, type, name, Ct));
    }

    /// <summary>Malformed bytes cannot be published.</summary>
    [Fact]
    public async Task Process_CorruptInput_Rejects()
    {
        using var input = new MemoryStream([1, 2, 3, 4]);
        await Should.ThrowAsync<DietPlannerDomainException>(() => new RecipeImageProcessor().ProcessAsync(input, "image/jpeg", "photo.jpg", Ct));
    }

    /// <summary>Input bytes are bounded before native decoding.</summary>
    [Fact]
    public async Task Process_OverByteLimit_RejectsBeforeDecode()
    {
        using var input = new MemoryStream(new byte[10 * 1024 * 1024 + 1]);
        await Should.ThrowAsync<DietPlannerDomainException>(() => new RecipeImageProcessor().ProcessAsync(input, "image/jpeg", "photo.jpg", Ct));
    }

    /// <summary>Dimensions are checked before allocating decoded pixels.</summary>
    [Fact]
    public async Task Process_OverPixelLimit_RejectsBeforePixelAllocation()
    {
        using var input = new MemoryStream(Photo(SKEncodedImageFormat.Png, 5001, 5000));
        await Should.ThrowAsync<DietPlannerDomainException>(() => new RecipeImageProcessor().ProcessAsync(input, "image/png", "photo.png", Ct));
    }

    /// <summary>Maximum-sized inputs retain the detail target after native subsampling or full PNG decoding.</summary>
    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, "image/jpeg", "photo.jpg")]
    [InlineData(SKEncodedImageFormat.Png, "image/png", "photo.png")]
    [InlineData(SKEncodedImageFormat.Webp, "image/webp", "photo.webp")]
    public async Task Process_MaximumPixelCount_ProducesBoundedDetail(SKEncodedImageFormat format, string type, string name)
    {
        using var input = new MemoryStream(Photo(format, 5000, 5000));
        ProcessedRecipeImage result = await new RecipeImageProcessor().ProcessAsync(input, type, name, Ct);
        result.Metadata.Width.ShouldBe(1600);
        result.Metadata.Height.ShouldBe(1600);
        using SKBitmap detail = SKBitmap.Decode(result.Detail);
        detail.Width.ShouldBe(1600);
        detail.GetPixel(800, 800).Red.ShouldBeGreaterThan((byte)200);
    }

    /// <summary>All eight EXIF orientations apply before source metadata is discarded.</summary>
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(6, true)]
    [InlineData(7, true)]
    [InlineData(8, true)]
    public async Task Process_ExifOrientation_AppliesAndStrips(int origin, bool swap)
    {
        byte[] jpeg = Photo(SKEncodedImageFormat.Jpeg, 80, 40);
        // APP1 Exif: little-endian TIFF with one SHORT Orientation entry.
        byte[] exif = [0xff, 0xe1, 0, 34, 69, 120, 105, 102, 0, 0, 73, 73, 42, 0, 8, 0, 0, 0,
            1, 0, 0x12, 1, 3, 0, 1, 0, 0, 0, (byte)origin, 0, 0, 0, 0, 0, 0, 0];
        using var input = new MemoryStream([.. jpeg[..2], .. exif, .. jpeg[2..]]);
        ProcessedRecipeImage result = await new RecipeImageProcessor().ProcessAsync(input, "image/jpeg", "photo.jpg", Ct);
        result.Metadata.Width.ShouldBe(swap ? 40 : 80);
        result.Metadata.Height.ShouldBe(swap ? 80 : 40);
        using var data = SKData.CreateCopy(result.Detail);
        using SKCodec codec = SKCodec.Create(data);
        codec.EncodedOrigin.ShouldBe(SKEncodedOrigin.TopLeft);
        System.Text.Encoding.ASCII.GetString(result.Detail).ShouldNotContain("Exif");
    }

    internal static byte[] Photo(SKEncodedImageFormat format = SKEncodedImageFormat.Jpeg, int width = 80, int height = 40)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(SKColors.Coral);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(format, 90);
        return data.ToArray();
    }

    /// <summary>Truncated decoder input cannot replace an active image.</summary>
    [Fact]
    public async Task Process_TruncatedJpeg_Rejects()
    {
        byte[] bytes = Photo();
        using var input = new MemoryStream(bytes[..(bytes.Length / 2)]);
        await Should.ThrowAsync<DietPlannerDomainException>(() => new RecipeImageProcessor().ProcessAsync(input, "image/jpeg", "photo.jpg", Ct));
    }

    /// <summary>APNG's animation marker is rejected even when a codec reports one frame.</summary>
    [Fact]
    public async Task Process_AnimatedPng_Rejects()
    {
        byte[] png = Photo(SKEncodedImageFormat.Png);
        byte[] chunk = [0, 0, 0, 8, 97, 99, 84, 76, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0];
        uint crc = 0xffffffff;
        foreach (byte value in chunk.AsSpan(4, 12))
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320 : 0);
        }
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(16), ~crc);
        using var input = new MemoryStream([.. png[..33], .. chunk, .. png[33..]]);
        await Should.ThrowAsync<DietPlannerDomainException>(() => new RecipeImageProcessor().ProcessAsync(input, "image/png", "photo.png", Ct));
    }

    /// <summary>Animated WebP containers are rejected, including a single-frame animation.</summary>
    [Fact]
    public async Task Process_AnimatedWebp_Rejects()
    {
        byte[] webp = Photo(SKEncodedImageFormat.Webp);
        byte[] animation = [86, 80, 56, 88, 10, 0, 0, 0, 2, 0, 0, 0, 79, 0, 0, 39, 0, 0,
            65, 78, 73, 77, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        byte[] animated = [.. webp[..12], .. animation, .. webp[12..]];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(animated.AsSpan(4), (uint)animated.Length - 8);
        using var input = new MemoryStream(animated);
        await Should.ThrowAsync<DietPlannerDomainException>(() => new RecipeImageProcessor().ProcessAsync(input, "image/webp", "photo.webp", Ct));
    }
}
