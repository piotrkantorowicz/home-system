namespace Shared.Abstractions.Core.Domain;

/// <summary>A dependency cannot serve the request. Module-specific failures may derive from this type.</summary>
/// <param name="message">A safe explanation containing no provider details.</param>
public class ServiceUnavailableException(string message) : Exception(message);
