using SecretFix.Core;

namespace SecretFix.Services;

public sealed class FeatureAccessService
{
    private readonly PlanTier _plan;
    private readonly AppLogService _log;

    public FeatureAccessService(PlanTier plan, AppLogService log)
    {
        _plan = plan;
        _log = log;
    }

    public bool IsAllowed(FeatureId feature) => FeatureCatalog.IsAllowed(_plan, feature);

    public bool EnsureAllowed(FeatureId feature, string action)
    {
        if (IsAllowed(feature))
            return true;

        var minimum = FeatureCatalog.MinimumPlan(feature).ToString().ToUpperInvariant();
        _log.Info($"Feature blocked. Feature={feature}; Action={action}; CurrentPlan={_plan}; RequiredPlan={minimum}");
        NotificationService.Show($"{action} requer o plano {minimum}.");
        return false;
    }
}
