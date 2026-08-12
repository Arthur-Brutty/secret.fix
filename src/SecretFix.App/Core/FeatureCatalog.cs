namespace SecretFix.Core;

public static class FeatureCatalog
{
    public static PlanTier MinimumPlan(FeatureId feature) => feature switch
    {
        FeatureId.MouseFix => PlanTier.Core,
        FeatureId.KeyboardFix or FeatureId.FiveM => PlanTier.Pulse,
        FeatureId.FlickTrainer or FeatureId.InputBenchmark or FeatureId.DisplayTuning or FeatureId.Diagnostics => PlanTier.Apex,
        _ => PlanTier.Apex
    };

    public static bool IsAllowed(PlanTier current, FeatureId feature) => current >= MinimumPlan(feature);
}
