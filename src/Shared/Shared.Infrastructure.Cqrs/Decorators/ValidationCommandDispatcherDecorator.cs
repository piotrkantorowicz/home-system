namespace Shared.Infrastructure.Cqrs.Decorators;

using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Cqrs;

internal sealed class ValidationCommandDispatcherDecorator : ICommandDispatcher
{
    private readonly ICommandDispatcher _inner;
    private readonly IServiceProvider _serviceProvider;

    public ValidationCommandDispatcherDecorator(
        ICommandDispatcher inner,
        IServiceProvider serviceProvider)
        => (_inner, _serviceProvider) = (inner, serviceProvider);

    public Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand
    {
        Validate(command);
        return _inner.SendAsync(command, ct);
    }

    public Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>
    {
        Validate(command);
        return _inner.SendAsync<TCommand, TResult>(command, ct);
    }

    private void Validate<TCommand>(TCommand command)
    {
        var validator = _serviceProvider.GetService<ICommandValidator<TCommand>>();
        if (validator is null)
            return;

        var errors = validator.Validate(command).ToList();
        if (errors.Count > 0)
            throw new CommandValidationException(typeof(TCommand).Name, errors);
    }
}
