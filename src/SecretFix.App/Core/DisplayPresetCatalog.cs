using SecretFix.State;

namespace SecretFix.Core;

public sealed record DisplayPresetValues(double Saturation, double Temperature, double Gamma);

public static class DisplayPresetCatalog
{
    public static bool TryGet(DisplayPreset preset, out DisplayPresetValues values)
    {
        values = preset switch
        {
            DisplayPreset.Normal => new DisplayPresetValues(50, 50, 50),
            DisplayPreset.Fps => new DisplayPresetValues(45, 50, 58),
            DisplayPreset.Vibrant => new DisplayPresetValues(72, 52, 55),
            _ => new DisplayPresetValues(0, 0, 0)
        };
        return preset != DisplayPreset.Custom;
    }

    public static void Apply(DisplayPreset preset, DisplayState state)
    {
        if (!TryGet(preset, out var values))
            return;

        state.Preset = preset;
        state.Saturation = values.Saturation;
        state.Temperature = values.Temperature;
        state.Gamma = values.Gamma;
    }
}
