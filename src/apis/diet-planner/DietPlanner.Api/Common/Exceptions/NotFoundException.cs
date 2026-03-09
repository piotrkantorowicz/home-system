namespace DietPlanner.Api.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object id)
        : base($"{entityName} with ID '{id}' was not found")
    {
    }
}
