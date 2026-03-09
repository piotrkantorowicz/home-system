namespace DietPlanner.Api.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }

    public ForbiddenException()
        : base("You don't have permission to access this resource")
    {
    }
}
