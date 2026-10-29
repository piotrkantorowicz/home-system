namespace Assistant.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Text.Json;
using Assistant.IntegrationTests.Infrastructure;
using Microsoft.Extensions.AI;

/// <summary>#649 — an image attached to a message reaches the model for that turn only and is never kept.</summary>
public sealed class AssistantImagesTests(AssistantDatabaseFixture databases) : IClassFixture<AssistantDatabaseFixture>, IDisposable
{
    // Smallest bytes that pass the signature checks; the scripted model never decodes them.
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F'];
    private static readonly byte[] Png = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly AssistantApiFactory _factory = new(databases);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>The image goes to the model as an image block next to the text; the history handed back keeps only a note.</summary>
    [Fact]
    public async Task SendMessage_WithImage_ReachesModelButNotHistory()
    {
        var me = _factory.CreateClientFor($"auth|{Guid.CreateVersion7():N}");

        List<SseItem<string>> events = await AssistantApiFactory.SendAsync(
            me, new { message = "What is on this receipt?", image = new { mediaType = "image/jpeg", data = Convert.ToBase64String(Jpeg) } });

        ChatMessage asked = _factory.Model.Calls.Single().Messages.Last(m => m.Role == ChatRole.User);
        asked.Text.ShouldBe("What is on this receipt?");
        DataContent image = asked.Contents.OfType<DataContent>().Single();
        image.MediaType.ShouldBe("image/jpeg");
        image.Data.ToArray().ShouldBe(Jpeg);

        string history = Data(events, "history").GetProperty("messages").GetRawText();
        history.ShouldNotContain(Convert.ToBase64String(Jpeg));
        history.ShouldContain("not kept");
    }

    /// <summary>The history that comes back with the next request is accepted (it carries no image).</summary>
    [Fact]
    public async Task SendMessage_HistoryFromImageTurn_IsAcceptedNextTime()
    {
        var me = _factory.CreateClientFor($"auth|{Guid.CreateVersion7():N}");
        List<SseItem<string>> first = await AssistantApiFactory.SendAsync(
            me, new { message = "Look", image = new { mediaType = "image/png", data = Convert.ToBase64String(Png) } });

        List<SseItem<string>> second = await AssistantApiFactory.SendAsync(
            me, new { message = "And now?", messages = Data(first, "history").GetProperty("messages") });

        second.Select(e => e.EventType).ShouldContain("done");
        _factory.Model.Calls.Last().Messages.SelectMany(m => m.Contents).OfType<DataContent>().ShouldBeEmpty();
    }

    /// <summary>Wrong type, bytes that are not the declared type, oversized, unreadable or message-less images are refused before the model is called.</summary>
    /// <param name="case">What is wrong with the image.</param>
    /// <param name="error">Part of the readable message.</param>
    [Theory]
    [InlineData("gif", "JPEG, PNG or WebP")]
    [InlineData("disguised", "could not be read")]
    [InlineData("oversized", "larger than")]
    [InlineData("not-base64", "could not be read")]
    [InlineData("no-message", "goes with a message")]
    public async Task SendMessage_BadImage_Returns400WithoutCallingModel(string @case, string error)
    {
        using var factory = new AssistantApiFactory(databases, new Dictionary<string, string?> { ["Assistant:MaxImageBytes"] = "64" });
        var me = factory.CreateClientFor($"auth|{Guid.CreateVersion7():N}");
        object body = @case switch
        {
            "gif" => new { message = "Hi", image = new { mediaType = "image/gif", data = Convert.ToBase64String(Jpeg) } },
            "disguised" => new { message = "Hi", image = new { mediaType = "image/png", data = Convert.ToBase64String(Jpeg) } },
            "oversized" => new { message = "Hi", image = new { mediaType = "image/jpeg", data = Convert.ToBase64String([.. Jpeg, .. new byte[100]]) } },
            "not-base64" => new { message = "Hi", image = new { mediaType = "image/jpeg", data = "not base64!" } },
            _ => (object)new { confirm = "x", image = new { mediaType = "image/jpeg", data = Convert.ToBase64String(Jpeg) } },
        };

        var response = await me.PostAsJsonAsync("/api/assistant/messages", body, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain(error);
        factory.Model.Calls.ShouldBeEmpty();
    }

    /// <summary>A text-only deployment says so in its status and refuses attachments.</summary>
    [Fact]
    public async Task ImagesDisabled_StatusSaysSoAndAttachmentIsRefused()
    {
        using var factory = new AssistantApiFactory(databases, new Dictionary<string, string?> { ["Assistant:ImagesEnabled"] = "false" });
        var me = factory.CreateClientFor($"auth|{Guid.CreateVersion7():N}");

        JsonElement status = await me.GetFromJsonAsync<JsonElement>("/api/assistant", Ct);
        var response = await me.PostAsJsonAsync(
            "/api/assistant/messages", new { message = "Hi", image = new { mediaType = "image/jpeg", data = Convert.ToBase64String(Jpeg) } }, Ct);

        status.GetProperty("images").GetBoolean().ShouldBeFalse();
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("does not accept images");
    }

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    private static JsonElement Data(List<SseItem<string>> events, string type)
        => JsonDocument.Parse(events.First(e => e.EventType == type).Data).RootElement;
}
