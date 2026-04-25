namespace Shared.Abstractions.Cqrs;

public interface ICommand { }
public interface ICommand<out TResult> { }
