namespace Assistant.Api;

/// <summary>A text file attached to a user message, such as a bank statement export (design §10). Passed to a tool for this turn only; never shown to the model or stored.</summary>
/// <param name="Name">File name, shown to the model in a note.</param>
/// <param name="Text">The file's full text; the panel decodes it (UTF-8, else Windows-1250).</param>
public sealed record AssistantDocument(string Name, string Text);
