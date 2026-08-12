namespace SecretFix.Core;

public static class FeatureCatalog
{
    public static PlanTier MinimumPlan(FeatureId feature) => feature switch
    {
        FeatureId.MouseFix => PlanTier.Core,
        FeatureId.Sensitivity => PlanTier.Core,
        FeatureId.KeyboardFix or FeatureId.FiveM or FeatureId.Services => PlanTier.Pulse,
        FeatureId.FlickTrainer or FeatureId.Aim or FeatureId.InputBenchmark or FeatureId.DisplayTuning or FeatureId.Diagnostics => PlanTier.Apex,
        _ => PlanTier.Apex
    };

    public static bool IsAllowed(PlanTier current, FeatureId feature) => current >= MinimumPlan(feature);
}
