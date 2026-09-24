namespace Shared.Web.IntegrationTests;

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;
using Shared.Web.IntegrationTests.Infrastructure;

/// <summary>
/// Pins the HTTP contract of <see cref="ApplicationExceptionHandler"/> through the real exception handler
/// middleware: the status mapping the old <c>ExceptionHandlingMiddleware</c> had, <c>application/problem+json</c>
/// bodies with a <c>traceId</c>, and framework-owned logging that fires once for faults and never for expected
/// exceptions.
/// </summary>
public sealed class ApplicationExceptionHandlerTests
{
    private const string ProblemJson = "application/problem+json";

    /// <summary>The exceptions that map to a problem body with a detail: (exception, status, title).</summary>
    public static TheoryData<Exception, HttpStatusCode, string> DetailedExceptions => new()
    {
        { new ForbiddenException("Only an owner can rename the household."), HttpStatusCode.Forbidden, "Forbidden" },
        { new NotFoundException("Household was not found."), HttpStatusCode.NotFound, "Not found" },
        { new DomainException("A household must keep at least one owner."), HttpStatusCode.UnprocessableEntity, "Business rule violation" },
    };

    /// <summary>403 / 404 / 422 carry the exception message as <c>detail</c>, plus the problem+json envelope.</summary>
    [Theory]
    [MemberData(nameof(DetailedExceptions))]
    public async Task TryHandle_WhenMappedException_WritesProblemDetailsWithDetail(
        Exception exception, HttpStatusCode status, string title)
    {
        await using var host = await ErrorPipelineHost.StartThrowingAsync(exception);
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(status);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemJson);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        body.RootElement.GetProperty("status").GetInt32().ShouldBe((int)status);
        body.RootElement.GetProperty("title").GetString().ShouldBe(title);
        body.RootElement.GetProperty("detail").GetString().ShouldBe(exception.Message);
        body.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>400 groups the validation errors by property under <c>errors</c>, like <c>ValidationProblemDetails</c>.</summary>
    [Fact]
    public async Task TryHandle_WhenCommandValidationException_Writes400WithErrorsGroupedByProperty()
    {
        var exception = new CommandValidationException(
            "CreateHouseholdCommand",
            [
                new ValidationError("Name", "Name is required."),
                new ValidationError("Name", "Name is too long."),
                new ValidationError("OwnerId", "OwnerId must not be empty."),
            ]);
        await using var host = await ErrorPipelineHost.StartThrowingAsync(exception);
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemJson);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        body.RootElement.GetProperty("title").GetString().ShouldBe("Validation failed");
        body.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();

        var errors = body.RootElement.GetProperty("errors");
        errors.GetProperty("Name").EnumerateArray().Select(e => e.GetString())
            .ShouldBe(["Name is required.", "Name is too long."]);
        errors.GetProperty("OwnerId").EnumerateArray().Select(e => e.GetString())
            .ShouldBe(["OwnerId must not be empty."]);
    }

    /// <summary>A cancelled request answers 499 with no body — the client is not there to read one.</summary>
    [Fact]
    public async Task TryHandle_WhenOperationCanceled_Returns499WithEmptyBody()
    {
        await using var host = await ErrorPipelineHost.StartThrowingAsync(new OperationCanceledException());
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        ((int)response.StatusCode).ShouldBe(ApplicationExceptionHandler.ClientClosedRequestStatusCode);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    /// <summary>An unexpected exception becomes a generic 500: no message, no type, no stack in the body.</summary>
    [Fact]
    public async Task TryHandle_WhenUnexpectedException_Returns500WithoutExceptionDetail()
    {
        var exception = new InvalidOperationException("connection string Password=hunter2 rejected");
        await using var host = await ErrorPipelineHost.StartThrowingAsync(exception);
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemJson);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldNotContain("hunter2");
        content.ShouldNotContain(nameof(InvalidOperationException));

        using var body = JsonDocument.Parse(content);
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(500);
        body.RootElement.GetProperty("title").GetString().ShouldBe("Internal server error");
        body.RootElement.TryGetProperty("detail", out _).ShouldBeFalse();
        body.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>The framework middleware logs a fault exactly once; the handler itself never logs.</summary>
    [Fact]
    public async Task TryHandle_WhenUnexpectedException_IsLoggedOnceAsErrorByTheFramework()
    {
        var exception = new InvalidOperationException("boom");
        await using var host = await ErrorPipelineHost.StartThrowingAsync(exception);
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        host.Logs.GetSnapshot()
            .Where(log => log.Level >= LogLevel.Error && ReferenceEquals(log.Exception, exception))
            .ShouldHaveSingleItem()
            .Category.ShouldStartWith("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware");
    }

    /// <summary>Expected exceptions are outcomes, not faults: nothing at Error level reaches the log.</summary>
    [Fact]
    public async Task TryHandle_WhenExpectedException_LogsNothingAtErrorLevel()
    {
        var exception = new NotFoundException("Household was not found.");
        await using var host = await ErrorPipelineHost.StartThrowingAsync(exception);
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        host.Logs.GetSnapshot().ShouldNotContain(log => log.Level >= LogLevel.Error);
    }
}
