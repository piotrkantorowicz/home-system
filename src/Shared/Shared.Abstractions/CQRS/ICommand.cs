namespace Shared.Abstractions.CQRS;

public interface ICommand { }
public interface ICommand<out TResult> { }
