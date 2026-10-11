namespace DietPlanner.Application.Storage;

/// <summary>An image stream and the resource that owns its lifetime.</summary>
/// <param name="content">The readable image stream.</param>
/// <param name="owner">The resource responsible for closing <paramref name="content"/> when disposed.</param>
public sealed class ImageObjectRead(Stream content, IDisposable owner) : IDisposable
{
    /// <summary>The streaming image content; valid until this handle is disposed.</summary>
    public Stream Content { get; } = content;

    /// <summary>Disposes the owner, which closes the content stream.</summary>
    public void Dispose() => owner.Dispose();
}
