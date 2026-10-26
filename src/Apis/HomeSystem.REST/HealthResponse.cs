namespace HomeSystem.REST;

/// <summary>Body of <c>GET /health</c>.</summary>
internal sealed record HealthResponse(string Status, DateTime Timestamp, string Version);
