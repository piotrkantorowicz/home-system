namespace Shared.Cqrs.Tests;

using System.Diagnostics.Metrics;

internal sealed class TestMeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = [];

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options.Name, options.Version, options.Tags, scope: this);
        _meters.Add(meter);
        return meter;
    }

    public void Dispose() => _meters.ForEach(m => m.Dispose());
}
