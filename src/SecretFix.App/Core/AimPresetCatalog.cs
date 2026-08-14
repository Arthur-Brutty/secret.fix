using SecretFix.State;

namespace SecretFix.Core;

public sealed record AimPresetValues(double Size, double Thickness, double Gap, double Opacity, string Color);

public static class AimPresetCatalog
{
    public static bool TryGet(AimPreset preset, out AimPresetValues values)
    {
        values = preset switch
        {
            AimPreset.Basic => new AimPresetValues(10, 2, 4, 100, "Red"),
            AimPreset.Medium => new AimPresetValues(18, 2.5, 6, 95, "Red"),
            AimPreset.High => new AimPresetValues(26, 3, 8, 100, "White"),
            _ => new AimPresetValues(0, 0, 0, 0, "Red")
        };
        return preset != AimPreset.Custom;
    }

    public static void Apply(AimPreset preset, AimState state)
    {
        if (!TryGet(preset, out var values))
            return;

        state.Preset = preset;
        state.Size = values.Size;
        state.Thickness = values.Thickness;
        state.Gap = values.Gap;
        state.Opacity = values.Opacity;
        state.Color = values.Color;
    }
}
