namespace Shared.Abstractions.Domain;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string entityName, object id)
        : base($"{entityName} with id '{id}' was not found.") { }

    public NotFoundException(string message)
        : base(message) { }
}
