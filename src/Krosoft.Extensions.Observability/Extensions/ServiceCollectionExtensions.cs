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
    /// L'endpoint, le protocole et l'échantillonnage sont lus par le SDK depuis les variables d'environnement standard
    /// (OTEL_EXPORTER_OTLP_ENDPOINT, OTEL_EXPORTER_OTLP_PROTOCOL, OTEL_TRACES_SAMPLER...) : rien n'est à configurer dans les appsettings.
    /// </summary>
    /// <param name="services">Collection de services.</param>
    /// <param name="configuration">Configuration de l'application.</param>
    /// <param name="logging">Constructeur de logs sur lequel brancher l'export OTLP.</param>
    /// <param name="action">Configuration des options : nom du service, signaux exportés, instrumentations.</param>
    /// <returns>Collection de services pour chaînage.</returns>
    public static IServiceCollection AddObservability(this IServiceCollection services,
                                                      IConfiguration configuration,
                                                      ILoggingBuilder logging,
                                                      Action<ObservabilityOptions> action)
    {
        Guard.IsNotNull(nameof(configuration), configuration);
        Guard.IsNotNull(nameof(logging), logging);
        Guard.IsNotNull(nameof(action), action);

        var options = new ObservabilityOptions();
        action(options);

        Guard.IsNotNullOrWhiteSpace(nameof(options.ServiceName), options.ServiceName);

        // Sans endpoint, rien n'est enregistré : les tests et le développement hors conteneur
        // ne doivent pas tenter d'exporter quoi que ce soit.
        if (string.IsNullOrWhiteSpace(configuration[ObservabilityConstants.Variables.Endpoint]))
        {
            return services;
        }

        // Aucun signal demandé : l'observabilité est désactivée, rien n'est enregistré.
        if (!options.IsTracingEnabled && !options.IsMetricsEnabled && !options.IsLoggingEnabled)
        {
            return services;
        }

        var environmentName = configuration[ObservabilityConstants.Variables.Environment];

        // La resource est configurée même si un seul signal est actif : elle porte le service.name
        // des traces, des métriques et des logs.
        var builder = services.AddOpenTelemetry()
                              .ConfigureResource(resource => ConfigureResource(resource, options.ServiceName, environmentName));

        if (options.IsTracingEnabled)
        {
            builder.WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation(o => o.Filter = IsNotHealthCheck)
                       .AddHttpClientInstrumentation()
                       .AddSource(ObservabilityConstants.Name)
                       .AddSource(options.ServiceName)
                       .AddOtlpExporter();

                options.ApplyTracing(tracing);
            });
        }

        if (options.IsMetricsEnabled)
        {
            builder.WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddRuntimeInstrumentation()
                       .AddMeter(ObservabilityConstants.Name)
                       .AddMeter(options.ServiceName)
                       .AddOtlpExporter();

                options.ApplyMetrics(metrics);
            });
        }

        if (options.IsLoggingEnabled)
        {
            logging.AddOpenTelemetry(o =>
            {
                // Sans ces deux options, les logs arrivent avec un corps vide et sans attributs.
                o.IncludeFormattedMessage = true;
                o.IncludeScopes = true;
                o.AddOtlpExporter();
            });
        }

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
