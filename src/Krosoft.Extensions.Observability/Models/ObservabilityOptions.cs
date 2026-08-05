using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Krosoft.Extensions.Observability.Models;

/// <summary>
/// Permet à l'application d'ajouter ses propres instrumentations sans que le package
/// ne référence en dur des dépendances dont toutes les applications n'ont pas besoin
/// (base de données, cache, messaging...).
/// </summary>
public sealed record ObservabilityOptions
{
    private readonly List<Action<MeterProviderBuilder>> _metrics = [];
    private readonly List<Action<TracerProviderBuilder>> _tracing = [];

    public ObservabilityOptions ConfigureTracing(Action<TracerProviderBuilder> configure)
    {
        _tracing.Add(configure);
        return this;
    }

    public ObservabilityOptions ConfigureMetrics(Action<MeterProviderBuilder> configure)
    {
        _metrics.Add(configure);
        return this;
    }

    internal void ApplyTracing(TracerProviderBuilder builder)
    {
        foreach (var configure in _tracing)
        {
            configure(builder);
        }
    }

    internal void ApplyMetrics(MeterProviderBuilder builder)
    {
        foreach (var configure in _metrics)
        {
            configure(builder);
        }
    }
}
