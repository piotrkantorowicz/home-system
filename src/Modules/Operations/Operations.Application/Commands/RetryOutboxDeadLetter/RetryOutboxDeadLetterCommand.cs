namespace Operations.Application.Commands.RetryOutboxDeadLetter;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Admin action: retries an undelivered outbox message as a new row; the original is kept as history.
/// </summary>
/// <param name="Module">Publishing module name.</param>
/// <param name="MessageId">The outbox row to retry.</param>
public sealed record RetryOutboxDeadLetterCommand(string Module, Guid MessageId) : ICommand;
