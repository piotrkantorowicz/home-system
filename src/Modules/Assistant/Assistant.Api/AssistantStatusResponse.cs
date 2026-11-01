namespace Assistant.Api;

/// <summary>What the panel needs before the first message: the assistant is on, and which provider it sends data to.</summary>
/// <param name="Provider">The configured model provider, shown in the panel (design §11).</param>
/// <param name="Images">The configured model accepts image attachments; the panel offers them only then.</param>
public sealed record AssistantStatusResponse(string Provider, bool Images);
