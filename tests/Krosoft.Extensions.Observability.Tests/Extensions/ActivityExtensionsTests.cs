using System.Diagnostics;
using Krosoft.Extensions.Observability.Extensions;
using Krosoft.Extensions.Observability.Models;
using Krosoft.Extensions.Testing;

namespace Krosoft.Extensions.Observability.Tests.Extensions;

[TestClass]
public class ActivityExtensionsTests : BaseTest
{
    private ActivityListener _listener = null!;

    [TestInitialize]
    public void SetUp()
    {
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
    public void AjoutDUnIdentifiantDeCorrelation_RenseigneLAttribut()
    {
        var correlationId = Guid.NewGuid();
        using var activity = ObservabilityConstants.ActivitySource.StartActivity("test");

        activity.SetCorrelationId(correlationId);

        Check.That(activity!.GetTagItem(ObservabilityConstants.Attributes.CorrelationId)).IsEqualTo(correlationId.ToString());
    }

    [TestMethod]
    public void AjoutDUnIdentifiantDeCorrelation_Null_NAjoutePasDAttribut()
    {
        using var activity = ObservabilityConstants.ActivitySource.StartActivity("test");

        activity.SetCorrelationId(null);

        Check.That(activity!.GetTagItem(ObservabilityConstants.Attributes.CorrelationId)).IsNull();
    }

    [TestMethod]
    public void AjoutDUnTenant_RenseigneLAttribut()
    {
        using var activity = ObservabilityConstants.ActivitySource.StartActivity("test");

        activity.SetTenantId(42);

        Check.That(activity!.GetTagItem(ObservabilityConstants.Attributes.TenantId)).IsEqualTo(42L);
    }

    [TestMethod]
    public void MiseEnErreur_PositionneLeStatutEtEnregistreLException()
    {
        using var activity = ObservabilityConstants.ActivitySource.StartActivity("test");

        activity.SetError(new InvalidOperationException("Boum"));

        Check.That(activity!.Status).IsEqualTo(ActivityStatusCode.Error);
        Check.That(activity.StatusDescription).IsEqualTo("Boum");
        Check.That(activity.Events.Any(e => e.Name == "exception")).IsTrue();
    }

    [TestMethod]
    public void MiseEnErreur_SansActivite_NeLevePasDErreur()
    {
        Activity? activity = null;

        Check.ThatCode(() => activity.SetError(new InvalidOperationException("Boum"))).DoesNotThrow();
    }
}
