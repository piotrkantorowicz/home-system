namespace Investments.Application.Fx;

/// <summary>NBP could not be reached, timed out, or answered with an error or an unreadable body.</summary>
public sealed class NbpUnavailableException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">What failed.</param>
    /// <param name="innerException">The underlying failure.</param>
    public NbpUnavailableException(string message, Exception? innerException = null) : base(message, innerException) { }
}
