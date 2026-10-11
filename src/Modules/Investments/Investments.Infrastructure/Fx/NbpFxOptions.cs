namespace Investments.Infrastructure.Fx;

/// <summary>Settings for the NBP rate worker, bound from <c>Investments:NbpFx</c>.</summary>
public sealed class NbpFxOptions
{
    /// <summary>Configuration section the options bind from.</summary>
    public const string SectionName = "Investments:NbpFx";

    /// <summary>Whether the worker runs; the integration-test host turns it off.</summary>
    public bool Enabled { get; init; } = true;
}
