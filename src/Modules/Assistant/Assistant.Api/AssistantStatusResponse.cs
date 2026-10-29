namespace Assistant.Api;

/// <summary>What the panel needs before the first message: the assistant is on, and which provider it sends data to.</summary>
/// <param name="Provider">The configured model provider, shown in the panel (design §11).</param>
public sealed record AssistantStatusResponse(string Provider);
