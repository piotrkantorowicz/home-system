namespace Assistant.Api;

using System.Text.Json;

/// <summary>One turn: a new user message, or the user's decision on a pending action. Exactly one of
/// <paramref name="Message"/>, <paramref name="Confirm"/> and <paramref name="Cancel"/> is set.</summary>
/// <param name="Message">The new user message.</param>
/// <param name="TimeZone">The user's IANA time zone (from <c>Intl</c>); UTC when omitted.</param>
/// <param name="Messages">Earlier turns as the <c>history</c> events returned them; held by the browser.</param>
/// <param name="Confirm">Token of the pending action to run.</param>
/// <param name="Cancel">Token of the pending action to decline.</param>
/// <param name="Image">An image attached to <paramref name="Message"/>; this turn only.</param>
/// <param name="Document">A text file attached to <paramref name="Message"/>; this turn only.</param>
public sealed record AssistantMessageRequest(
    string? Message = null,
    string? TimeZone = null,
    JsonElement? Messages = null,
    string? Confirm = null,
    string? Cancel = null,
    AssistantImage? Image = null,
    AssistantDocument? Document = null);
