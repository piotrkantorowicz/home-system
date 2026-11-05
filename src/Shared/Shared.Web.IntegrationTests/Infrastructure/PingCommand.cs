namespace Shared.Web.IntegrationTests.Infrastructure;

using Shared.Abstractions.Cqrs;

/// <summary>Command <c>/commands/ping</c> dispatches, so the CQRS span runs inside a real request span.</summary>
internal sealed record PingCommand : ICommand;
