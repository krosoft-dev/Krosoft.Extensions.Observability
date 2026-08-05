using Krosoft.Extensions.Core.Models.Exceptions;
using Krosoft.Extensions.Observability.Extensions;
using Krosoft.Extensions.Observability.Models;
using Krosoft.Extensions.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Krosoft.Extensions.Observability.Tests.Extensions;

[TestClass]
public class ServiceCollectionExtensionsTests : BaseTest
{
    private const string Endpoint = "http://localhost:4317";

    [TestMethod]
    public void AjoutDeLObservabilite_SansEndpoint_NEnregistreAucunFournisseur()
    {
        var services = Add(null);

        Check.That(services.Any(s => s.ServiceType == typeof(TracerProvider))).IsFalse();
        Check.That(services.Any(s => s.ServiceType == typeof(MeterProvider))).IsFalse();
        Check.That(services.Any(s => s.ServiceType == typeof(ILoggerProvider))).IsFalse();
    }

    [TestMethod]
    public void AjoutDeLObservabilite_AvecEndpoint_EnregistreLesTracesEtLesMetriques()
    {
        var services = Add(Endpoint);

        Check.That(services.Any(s => s.ServiceType == typeof(TracerProvider))).IsTrue();
        Check.That(services.Any(s => s.ServiceType == typeof(MeterProvider))).IsTrue();
    }

    [TestMethod]
    public void AjoutDeLObservabilite_AvecEndpoint_EnregistreLeFournisseurDeLogs()
    {
        var services = Add(Endpoint);

        Check.That(services.Where(s => s.ServiceType == typeof(ILoggerProvider))).HasSize(1);
    }

    [TestMethod]
    public void AjoutDeLObservabilite_AvecUneConfigurationSpecifique_LApplique()
    {
        var appele = false;

        Add(Endpoint, options => options.ConfigureTracing(_ => appele = true));

        Check.That(appele).IsTrue();
    }

    [TestMethod]
    public void AjoutDeLObservabilite_SansEndpoint_NAppliquePasLaConfigurationSpecifique()
    {
        var appele = false;

        Add(null, options => options.ConfigureTracing(_ => appele = true));

        Check.That(appele).IsFalse();
    }

    [TestMethod]
    public void AjoutDeLObservabilite_SansNomDeService_LeveUneErreur()
        => Check.ThatCode(() => Add(Endpoint, serviceName: " "))
                .Throws<KrosoftTechnicalException>();

    [TestMethod]
    public void AjoutDeLObservabilite_RetourneLaMemeCollectionDeServices()
    {
        var services = new ServiceCollection();
        IServiceCollection? resultat = null;

        services.AddLogging(logging => resultat = services.AddObservability(CreateConfiguration(Endpoint), logging, "test-service"));

        Check.That(resultat).IsSameReferenceAs(services);
    }

    private static IServiceCollection Add(string? endpoint,
                                          Action<ObservabilityOptions>? configure = null,
                                          string serviceName = "test-service")
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => services.AddObservability(CreateConfiguration(endpoint), logging, serviceName, configure));

        return services;
    }

    private static IConfiguration CreateConfiguration(string? endpoint)
        => new ConfigurationBuilder()
           .AddInMemoryCollection(new Dictionary<string, string?>
           {
               [ObservabilityConstants.Variables.Endpoint] = endpoint,
               [ObservabilityConstants.Variables.Environment] = "Test"
           })
           .Build();
}
