using System.Reflection;
using Krosoft.Extensions.Core.Tools;
using Krosoft.Extensions.Observability.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Krosoft.Extensions.Observability.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Exporte les logs, les traces et les métriques en OTLP.
    /// L'endpoint, le protocole et l'échantillonnage sont lus par le SDK depuis les variables
    /// d'environnement standard (OTEL_EXPORTER_OTLP_ENDPOINT, OTEL_EXPORTER_OTLP_PROTOCOL,
    /// OTEL_TRACES_SAMPLER...) : rien n'est à configurer dans les appsettings.
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services,
                                                      IConfiguration configuration,
                                                      ILoggingBuilder logging,
                                                      string serviceName,
                                                      Action<ObservabilityOptions>? configure = null)
    {
        Guard.IsNotNull(nameof(configuration), configuration);
        Guard.IsNotNull(nameof(logging), logging);
        Guard.IsNotNullOrWhiteSpace(nameof(serviceName), serviceName);

        // Sans endpoint, rien n'est enregistré : les tests et le développement hors conteneur
        // ne doivent pas tenter d'exporter quoi que ce soit.
        if (string.IsNullOrWhiteSpace(configuration[ObservabilityConstants.Variables.Endpoint]))
        {
            return services;
        }

        var options = new ObservabilityOptions();
        configure?.Invoke(options);

        var environmentName = configuration[ObservabilityConstants.Variables.Environment];

        services.AddOpenTelemetry()
                .ConfigureResource(resource => ConfigureResource(resource, serviceName, environmentName))
                .WithTracing(tracing =>
                {
                    tracing.AddAspNetCoreInstrumentation(o => o.Filter = IsNotHealthCheck)
                           .AddHttpClientInstrumentation()
                           .AddSource(ObservabilityConstants.Name)
                           .AddSource(serviceName)
                           .AddOtlpExporter();

                    options.ApplyTracing(tracing);
                })
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                           .AddHttpClientInstrumentation()
                           .AddRuntimeInstrumentation()
                           .AddMeter(ObservabilityConstants.Name)
                           .AddMeter(serviceName)
                           .AddOtlpExporter();

                    options.ApplyMetrics(metrics);
                });

        logging.AddOpenTelemetry(o =>
        {
            // Sans ces deux options, les logs arrivent avec un corps vide et sans attributs.
            o.IncludeFormattedMessage = true;
            o.IncludeScopes = true;
            o.AddOtlpExporter();
        });

        return services;
    }

    private static void ConfigureResource(ResourceBuilder resource, string serviceName, string? environmentName)
    {
        resource.AddService(serviceName,
                            serviceVersion: Assembly.GetEntryAssembly()?.GetName().Version?.ToString(),
                            serviceInstanceId: Environment.MachineName);

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            resource.AddAttributes([new KeyValuePair<string, object>(ObservabilityConstants.Attributes.DeploymentEnvironment, environmentName)]);
        }
    }

    /// <summary>
    /// Les sondes de santé sont appelées en continu par l'orchestrateur et le reverse proxy :
    /// sans ce filtre, elles représentent l'essentiel des traces.
    /// </summary>
    private static bool IsNotHealthCheck(HttpContext context)
        => !context.Request.Path.StartsWithSegments(ObservabilityConstants.Paths.Health, StringComparison.OrdinalIgnoreCase);
}
