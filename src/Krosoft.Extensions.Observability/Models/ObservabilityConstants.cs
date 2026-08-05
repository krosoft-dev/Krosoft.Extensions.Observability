using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Krosoft.Extensions.Observability.Models;

public static class ObservabilityConstants
{
    /// <summary>
    /// Source d'activités et compteur partagés, enregistrés d'office par AddObservability.
    /// </summary>
    public const string Name = "Krosoft";

    public static readonly ActivitySource ActivitySource = new(Name);

    public static readonly Meter Meter = new(Name);

    public static class Variables
    {
        public const string Endpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
        public const string Environment = "DOTNET_ENVIRONMENT";
    }

    public static class Attributes
    {
        public const string CorrelationId = "correlation_id";
        public const string TenantId = "tenant_id";
        public const string DeploymentEnvironment = "deployment.environment";
    }

    public static class Paths
    {
        public const string Health = "/health";
    }
}
