namespace Assistant.Api;

/// <summary>An image attached to a user message (design §10). Sent to the model for this turn only; never stored.</summary>
/// <param name="MediaType"><c>image/jpeg</c>, <c>image/png</c> or <c>image/webp</c>.</param>
/// <param name="Data">The image bytes, base64-encoded.</param>
public sealed record AssistantImage(string MediaType, string Data);
