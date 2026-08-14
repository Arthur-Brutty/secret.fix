using SecretFix.Core;
using SecretFix.State;

namespace SecretFix.Tests;

public sealed class PresetTests
{
    [Theory]
    [InlineData(AimPreset.Basic, 10, 2, 4, 100, "Red")]
    [InlineData(AimPreset.Medium, 18, 2.5, 6, 95, "Red")]
    [InlineData(AimPreset.High, 26, 3, 8, 100, "White")]
    public void AimPreset_AppliesRealParameters(AimPreset preset, double size, double thickness, double gap, double opacity, string color)
    {
        var state = new AimState();

        AimPresetCatalog.Apply(preset, state);

        Assert.Equal(preset, state.Preset);
        Assert.Equal(size, state.Size);
        Assert.Equal(thickness, state.Thickness);
        Assert.Equal(gap, state.Gap);
        Assert.Equal(opacity, state.Opacity);
        Assert.Equal(color, state.Color);
    }

    [Fact]
    public void CustomAim_DoesNotOverwriteManualValues()
    {
        var state = new AimState { Preset = AimPreset.Custom, Size = 41, Thickness = 4, Gap = 3, Opacity = 77, Color = "White" };

        AimPresetCatalog.Apply(AimPreset.Custom, state);

        Assert.Equal(41, state.Size);
        Assert.Equal(4, state.Thickness);
        Assert.Equal(3, state.Gap);
        Assert.Equal(77, state.Opacity);
        Assert.Equal("White", state.Color);
    }
}
