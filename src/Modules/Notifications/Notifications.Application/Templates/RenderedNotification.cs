namespace Notifications.Application.Templates;

/// <summary>
/// A template with its placeholders filled in.
/// </summary>
/// <param name="Title">Final title.</param>
/// <param name="Body">Final body.</param>
public sealed record RenderedNotification(string Title, string Body);
