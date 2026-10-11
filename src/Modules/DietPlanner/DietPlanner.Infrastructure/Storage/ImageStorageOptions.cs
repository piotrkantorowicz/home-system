namespace DietPlanner.Infrastructure.Storage;

/// <summary>Credential-free configuration for optional private image storage.</summary>
public sealed class ImageStorageOptions
{
    /// <summary>Configuration section containing image storage settings.</summary>
    public const string SectionName = "DietPlanner:ImageStorage";

    /// <summary>Whether image storage is enabled; disabled by default.</summary>
    public bool Enabled { get; set; }

    /// <summary>Optional HTTP(S) origin; empty selects the regional AWS endpoint.</summary>
    public string? Endpoint { get; set; }

    /// <summary>The signing region.</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>The private bucket's DNS-style name.</summary>
    public string Bucket { get; set; } = "";

    /// <summary>Whether the bucket is addressed in the URL path.</summary>
    public bool ForcePathStyle { get; set; }
}
