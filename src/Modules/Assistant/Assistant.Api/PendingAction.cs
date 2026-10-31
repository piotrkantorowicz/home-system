namespace Assistant.Api;

using System.Text.Json;

/// <summary>A write the model proposed and the server holds until the user confirms or cancels it.</summary>
/// <param name="Subject">Auth subject of the user it was proposed to.</param>
/// <param name="Tool">Tool name.</param>
/// <param name="CallId">The model's call id; the result answers this call.</param>
/// <param name="Arguments">Exactly the arguments confirm runs, operation key included.</param>
/// <param name="Reasoning">Provider reasoning that came with the call; confirm puts it back before the call, as the provider requires.</param>
/// <param name="ExpiresAt">After this the token is refused.</param>
internal sealed record PendingAction(string Subject, string Tool, string CallId, JsonElement Arguments, JsonElement? Reasoning, DateTimeOffset ExpiresAt);
