namespace Assistant.Api;

using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Signs held writes into tokens the panel sends back with Confirm or Cancel (design §6). Data
/// Protection encrypts and authenticates the token, so the browser can neither read nor alter the
/// arguments, tool or user it carries; confirm runs what is inside, never anything sent beside it.
/// </summary>
/// <param name="protection">The host's Data Protection key ring.</param>
/// <param name="clock">Expiry clock.</param>
internal sealed class PendingActions(IDataProtectionProvider protection, TimeProvider clock)
{
    /// <summary>How long a held action can be confirmed.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private readonly IDataProtector _protector = protection.CreateProtector("Assistant.PendingAction.v1");

    /// <summary>Holds one call.</summary>
    /// <param name="subject">The user it is proposed to.</param>
    /// <param name="tool">Tool name.</param>
    /// <param name="callId">The model's call id.</param>
    /// <param name="arguments">The arguments to run on confirm.</param>
    /// <param name="reasoning">Reasoning content that came with the call, if any.</param>
    /// <returns>The token.</returns>
    public string Hold(string subject, string tool, string callId, JsonElement arguments, JsonElement? reasoning)
        => _protector.Protect(JsonSerializer.Serialize(new PendingAction(subject, tool, callId, arguments, reasoning, clock.GetUtcNow() + Lifetime)));

    /// <summary>Opens a token for <paramref name="subject"/>.</summary>
    /// <param name="token">The token from the panel.</param>
    /// <param name="subject">The caller.</param>
    /// <returns>The held action, or why the token cannot be used.</returns>
    public (PendingAction? Action, string? Error) Read(string token, string subject)
    {
        PendingAction? action;
        try
        {
            action = JsonSerializer.Deserialize<PendingAction>(_protector.Unprotect(token));
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException)
        {
            return (null, "The pending action is invalid or from before a restart; ask the assistant again.");
        }

        if (action is null || action.Subject != subject)
            return (null, "The pending action belongs to someone else.");
        if (action.ExpiresAt <= clock.GetUtcNow())
            return (null, "The pending action has expired; ask the assistant again.");
        return (action, null);
    }
}
