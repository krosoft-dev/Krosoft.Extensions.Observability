using System.Diagnostics;
using Krosoft.Extensions.Core.Tools;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Krosoft.Extensions.Observability.Services;

/// <summary>
/// Propagation du contexte de trace W3C sur un porteur quelconque (propriétés d'un message,
/// en-têtes...), sans dépendance à un transport particulier.
/// </summary>
public static class TelemetryPropagation
{
    /// <summary>
    /// Écrit le contexte de l'activité courante (traceparent, tracestate, baggage) dans le porteur.
    /// Ne fait rien si aucune activité n'est en cours.
    /// </summary>
    public static void Inject(IDictionary<string, string> carrier)
    {
        Guard.IsNotNull(nameof(carrier), carrier);

        var activity = Activity.Current;
        if (activity == null)
        {
            return;
        }

        var context = new PropagationContext(activity.Context, Baggage.Current);
        Propagators.DefaultTextMapPropagator.Inject(context, carrier, static (c, key, value) => c[key] = value);
    }

    /// <summary>
    /// Relit le contexte de trace depuis un porteur typé <c>string</c>.
    /// </summary>
    public static PropagationContext Extract(IDictionary<string, string> carrier)
    {
        Guard.IsNotNull(nameof(carrier), carrier);

        return Propagators.DefaultTextMapPropagator.Extract(default, carrier, GetValues);

        static IEnumerable<string>? GetValues(IDictionary<string, string> c, string key)
            => c.TryGetValue(key, out var value) && value != null ? [value] : null;
    }

    /// <summary>
    /// Relit le contexte de trace depuis un porteur faiblement typé, comme les propriétés
    /// applicatives d'un message AMQP.
    /// </summary>
    public static PropagationContext Extract(IDictionary<string, object> carrier)
    {
        Guard.IsNotNull(nameof(carrier), carrier);

        return Propagators.DefaultTextMapPropagator.Extract(default, carrier, GetValues);

        static IEnumerable<string>? GetValues(IDictionary<string, object> c, string key)
            => c.TryGetValue(key, out var value) && value != null ? [Convert.ToString(value) ?? string.Empty] : null;
    }
}
