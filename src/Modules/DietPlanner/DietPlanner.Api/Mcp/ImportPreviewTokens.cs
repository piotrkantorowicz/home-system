namespace DietPlanner.Api.Mcp;

using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Issues and checks the token that binds an executable import preview to who asked for it, the
/// collision policy and the exact resolution (<c>ImportPreviewDto.Fingerprint</c>). The token is
/// encrypted and authenticated by ASP.NET Core Data Protection, so a client can neither read nor
/// forge or alter it. It proves the server produced this preview, not that a human approved it.
/// </summary>
internal sealed class ImportPreviewTokens(IDataProtectionProvider protection, TimeProvider clock)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private readonly IDataProtector _protector = protection.CreateProtector("DietPlanner.Mcp.ImportPreview.v1");

    public (string Token, DateTimeOffset ExpiresAt) Issue(string actorSubject, Guid personId, bool appendToOccupiedSlots, string fingerprint)
    {
        var expiresAt = clock.GetUtcNow() + Lifetime;
        var payload = new Payload(actorSubject, personId, appendToOccupiedSlots, fingerprint, expiresAt);
        return (_protector.Protect(JsonSerializer.Serialize(payload)), expiresAt);
    }

    /// <summary>Checks a token against the current request and the fingerprint of a fresh resolution.</summary>
    /// <returns><see langword="null"/> when valid; otherwise why the preview cannot be used.</returns>
    public string? Check(string token, string actorSubject, Guid personId, bool appendToOccupiedSlots, string fingerprint)
    {
        Payload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Payload>(_protector.Unprotect(token));
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
        {
            return "The preview token is invalid.";
        }

        if (payload is null || payload.ActorSubject != actorSubject || payload.PersonId != personId)
            return "The preview token belongs to someone else.";
        if (payload.ExpiresAt <= clock.GetUtcNow())
            return "The preview token has expired; preview the plan again.";
        if (payload.AppendToOccupiedSlots != appendToOccupiedSlots || payload.Fingerprint != fingerprint)
            return "The plan, its options or the data it depends on changed since the preview; preview the plan again.";
        return null;
    }

    private sealed record Payload(string ActorSubject, Guid PersonId, bool AppendToOccupiedSlots, string Fingerprint, DateTimeOffset ExpiresAt);
}
