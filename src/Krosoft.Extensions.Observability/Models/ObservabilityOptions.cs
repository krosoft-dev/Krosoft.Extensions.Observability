using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Krosoft.Extensions.Observability.Models;

/// <summary>
/// Options de configuration de l'observabilité : nom du service, signaux exportés et
/// instrumentations propres à l'application, afin que le package ne référence pas en dur
/// des dépendances dont toutes les applications n'ont pas besoin (base de données, cache, messaging...).
/// </summary>
public sealed record ObservabilityOptions
{
    private readonly List<Action<MeterProviderBuilder>> _metrics = [];
    private readonly List<Action<TracerProviderBuilder>> _tracing = [];

    /// <summary>
    /// Nom du service remonté dans les traces, les métriques et les logs.
    /// Il sert aussi de source d'activités et de compteur pour l'application. Obligatoire.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Exporte les traces. Actif par défaut.
    /// </summary>
    public bool IsTracingEnabled { get; set; } = true;

    /// <summary>
    /// Exporte les métriques. Actif par défaut.
    /// </summary>
    public bool IsMetricsEnabled { get; set; } = true;

    /// <summary>
    /// Exporte les logs. Actif par défaut.
    /// </summary>
    public bool IsLoggingEnabled { get; set; } = true;

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
