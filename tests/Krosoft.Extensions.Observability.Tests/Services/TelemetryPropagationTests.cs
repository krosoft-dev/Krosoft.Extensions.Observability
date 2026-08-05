using System.Diagnostics;
using Krosoft.Extensions.Observability.Models;
using Krosoft.Extensions.Observability.Services;
using Krosoft.Extensions.Testing;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace Krosoft.Extensions.Observability.Tests.Services;

[TestClass]
public class TelemetryPropagationTests : BaseTest
{
    private const string Traceparent = "traceparent";

    private ActivityListener _listener = null!;

    [TestInitialize]
    public void SetUp()
    {
        // Hors hébergement, le SDK n'a pas encore posé de propagateur ni de listener :
        // sans ces deux éléments, aucune activité n'est créée et rien n'est propagé.
        Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator([new TraceContextPropagator(), new BaggagePropagator()]));

        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ObservabilityConstants.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };

        ActivitySource.AddActivityListener(_listener);
    }

    [TestCleanup]
    public void TearDown() => _listener.Dispose();

    [TestMethod]
    public void Injection_SansActiviteEnCours_NeModifiePasLePorteur()
    {
        var porteur = new Dictionary<string, string>();

        TelemetryPropagation.Inject(porteur);

        Check.That(porteur).IsEmpty();
    }

    [TestMethod]
    public void Injection_AvecUneActiviteEnCours_EcritLeTraceparent()
    {
        using var activity = ObservabilityConstants.ActivitySource.StartActivity("test");
        var porteur = new Dictionary<string, string>();

        TelemetryPropagation.Inject(porteur);

        Check.That(activity).IsNotNull();
        Check.That(porteur).ContainsKey(Traceparent);
        Check.That(porteur[Traceparent]).Contains(activity!.TraceId.ToHexString());
    }

    [TestMethod]
    public void InjectionPuisExtraction_ConserveLIdentifiantDeTrace()
    {
        using var activity = ObservabilityConstants.ActivitySource.StartActivity("test");
        var porteur = new Dictionary<string, string>();
        TelemetryPropagation.Inject(porteur);

        var contexte = TelemetryPropagation.Extract(porteur);

        Check.That(contexte.ActivityContext.TraceId).IsEqualTo(activity!.TraceId);
        Check.That(contexte.ActivityContext.SpanId).IsEqualTo(activity.SpanId);
    }

    [TestMethod]
    public void Extraction_DUnPorteurFaiblementType_ConserveLIdentifiantDeTrace()
    {
        using var activity = ObservabilityConstants.ActivitySource.StartActivity("test");
        var porteur = new Dictionary<string, string>();
        TelemetryPropagation.Inject(porteur);

        // Les propriétés applicatives d'un message AMQP sont typées en object.
        var porteurAmqp = porteur.ToDictionary(x => x.Key, object (x) => x.Value);

        var contexte = TelemetryPropagation.Extract(porteurAmqp);

        Check.That(contexte.ActivityContext.TraceId).IsEqualTo(activity!.TraceId);
    }

    [TestMethod]
    public void Extraction_DUnPorteurVide_RetourneUnContexteVide()
    {
        var contexte = TelemetryPropagation.Extract(new Dictionary<string, string>());

        Check.That(contexte.ActivityContext.TraceId).IsEqualTo(default(ActivityTraceId));
    }
}
