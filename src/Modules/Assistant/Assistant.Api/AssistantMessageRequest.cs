namespace Assistant.Api;

using System.Text.Json;

/// <summary>One user turn.</summary>
/// <param name="Message">The new user message.</param>
/// <param name="TimeZone">The user's IANA time zone (from <c>Intl</c>); UTC when omitted.</param>
/// <param name="Messages">Earlier turns as the <c>history</c> events returned them; held by the browser.</param>
public sealed record AssistantMessageRequest(string Message, string? TimeZone = null, JsonElement? Messages = null);
