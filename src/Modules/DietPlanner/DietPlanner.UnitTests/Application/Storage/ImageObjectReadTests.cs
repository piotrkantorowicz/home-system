namespace DietPlanner.UnitTests.Application.Storage;

using DietPlanner.Application.Storage;

/// <summary>Verifies ownership of streaming image reads.</summary>
public sealed class ImageObjectReadTests
{
    /// <summary>Disposing the handle closes the stream owned by its response.</summary>
    [Fact]
    public void Dispose_WhenHandleOwnsStream_ClosesStream()
    {
        using var content = new MemoryStream([1, 2, 3]);
        using var read = new ImageObjectRead(content, content);
        read.Content.ShouldBeSameAs(content);

        read.Dispose();

        content.CanRead.ShouldBeFalse();
    }
}
