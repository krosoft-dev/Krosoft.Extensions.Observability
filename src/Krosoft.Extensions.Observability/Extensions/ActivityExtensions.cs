using System.Diagnostics;
using Krosoft.Extensions.Observability.Models;

namespace Krosoft.Extensions.Observability.Extensions;

public static class ActivityExtensions
{
    /// <summary>
    /// Expose l'identifiant de corrélation métier comme attribut de la trace : c'est la clé
    /// qui permet de passer des données applicatives à la trace, et inversement.
    /// </summary>
    public static Activity? SetCorrelationId(this Activity? activity, string? correlationId)
        => string.IsNullOrWhiteSpace(correlationId)
            ? activity
            : activity?.SetTag(ObservabilityConstants.Attributes.CorrelationId, correlationId);

    public static Activity? SetTenantId(this Activity? activity, string? tenantId)
        => string.IsNullOrWhiteSpace(tenantId)
            ? activity
            : activity?.SetTag(ObservabilityConstants.Attributes.TenantId, tenantId);

    /// <summary>
    /// Marque l'activité en erreur. Indispensable quand l'exception est rattrapée sans être
    /// relancée : sans cet appel, la trace apparaît en succès alors que le traitement a échoué.
    /// </summary>
    public static Activity? SetError(this Activity? activity, Exception exception)
    {
        if (activity == null)
        {
            return null;
        }

        activity.AddException(exception);

        return activity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }
}
