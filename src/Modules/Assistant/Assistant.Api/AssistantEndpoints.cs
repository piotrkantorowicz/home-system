namespace Assistant.Api;

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.AI;

/// <summary>The assistant endpoint: SPA token in, server-sent events out.</summary>
internal static class AssistantEndpoints
{
    private const int MaxMessageLength = 8000;

    // History is untrusted: only the roles and content the loop itself produces are accepted. A system
    // message from the browser would speak with operator authority, so it is rejected.
    private static readonly HashSet<ChatRole> HistoryRoles = [ChatRole.User, ChatRole.Assistant, ChatRole.Tool];
    private static readonly HashSet<Type> HistoryContents =
        [typeof(TextContent), typeof(TextReasoningContent), typeof(FunctionCallContent), typeof(FunctionResultContent)];

    internal static IEndpointRouteBuilder MapAssistantEndpointsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/assistant")
            .RequireAuthorization()
            .WithTags("Assistant");

        group.MapGet("", GetStatus)
            .WithName("GetAssistantStatus")
            .WithSummary("Report that the assistant is on and which provider it uses; 404 when it is off");

        group.MapPost("/messages", SendMessage)
            .WithName("SendAssistantMessage")
            .WithSummary("Run one assistant turn and stream text, tool activity, history and usage as server-sent events");

        return app;
    }

    private static Ok<AssistantStatusResponse> GetStatus(AssistantOptions options)
        => TypedResults.Ok(new AssistantStatusResponse(options.Provider));

    private static Results<ServerSentEventsResult<object>, ValidationProblem, ProblemHttpResult> SendMessage(
        AssistantMessageRequest request, ClaimsPrincipal user, AssistantAgent agent, AssistantUsage usage, PendingActions pending,
        CancellationToken ct)
    {
        Dictionary<string, string[]> errors = [];
        string subject = AssistantUsage.SubjectOf(user);

        // A decision runs exactly the action inside the token; nothing else in the request changes it.
        PendingAction? action = null;
        string? token = request.Confirm ?? request.Cancel;
        int parts = new[] { request.Message, request.Confirm, request.Cancel }.Count(p => p is not null);
        if (parts != 1)
        {
            errors["message"] = ["Send exactly one of message, confirm or cancel."];
        }
        else if (token is not null)
        {
            (action, string? tokenError) = pending.Read(token, subject);
            if (tokenError is not null)
                errors[request.Confirm is null ? "cancel" : "confirm"] = [tokenError];
        }
        else if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > MaxMessageLength)
        {
            errors["message"] = [$"Message is required and at most {MaxMessageLength} characters."];
        }

        TimeZoneInfo zone = TimeZoneInfo.Utc;
        if (request.TimeZone is { } id && !TimeZoneInfo.TryFindSystemTimeZoneById(id, out zone!))
            errors["timeZone"] = ["Unknown time zone."];

        IReadOnlyList<ChatMessage>? history = ParseHistory(request.Messages);
        if (history is null || !history.All(IsAllowed))
            errors["messages"] = ["History contains a message the assistant does not accept."];

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        // Over budget or over rate: refused before the model is called.
        if (usage.TryStartRequest(subject) is { } refusal)
            return TypedResults.Problem(refusal, statusCode: StatusCodes.Status429TooManyRequests, title: "Assistant limit reached");

        // history is non-null and exactly one of message / action is set when there are no errors
        return TypedResults.ServerSentEvents(action is null
            ? agent.RunAsync(user, history!, request.Message!, zone, ct)
            : agent.DecideAsync(user, history!, action, request.Confirm is not null, zone, ct));
    }

    // Null when the history is not a list of messages.
    private static List<ChatMessage>? ParseHistory(JsonElement? messages)
    {
        if (messages is null or { ValueKind: JsonValueKind.Null })
            return [];

        try
        {
            return messages.Value.Deserialize<List<ChatMessage>>(AIJsonUtilities.DefaultOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // JSON nulls deserialize into the list despite its non-nullable type, so check for them.
    private static bool IsAllowed(ChatMessage? message)
        => message is not null
           && HistoryRoles.Contains(message.Role)
           && message.Contents.All(c => c is not null && HistoryContents.Contains(c.GetType()));
}
