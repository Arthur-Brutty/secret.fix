using SecretFix.Core;
using SecretFix.Services;

namespace SecretFix.Tests;

public sealed class DeviceAndFeatureTests
{
    [Theory]
    [InlineData(@"HID\VID_046D&PID_C09B&MI_00", "046D", "C09B")]
    [InlineData(@"hid\vid_1532&pid_00b6", "1532", "00B6")]
    public void ParseVidPid_NormalizesIdentifiers(string hardwareId, string expectedVid, string expectedPid)
    {
        var (vid, pid) = DeviceDetectionService.ParseVidPid(hardwareId);

        Assert.Equal(expectedVid, vid);
        Assert.Equal(expectedPid, pid);
    }

    [Fact]
    public void ParseVidPid_ReturnsNullForGenericDescription()
    {
        var (vid, pid) = DeviceDetectionService.ParseVidPid("HID-compliant mouse");

        Assert.Null(vid);
        Assert.Null(pid);
    }

    [Fact]
    public void KnownDevices_MatchesExactVidPidOnly()
    {
        var matched = KnownDevices.Match(DeviceKind.Mouse, "046d", "c09b");

        Assert.NotNull(matched);
        Assert.Equal("G Pro X Superlight 2", matched.Model);
        Assert.Null(KnownDevices.Match(DeviceKind.Mouse, "046D", "FFFF"));
        Assert.Null(KnownDevices.Match(DeviceKind.Keyboard, "046D", "C09B"));
    }

    [Theory]
    [InlineData(PlanTier.Core, FeatureId.MouseFix, true)]
    [InlineData(PlanTier.Core, FeatureId.FiveM, false)]
    [InlineData(PlanTier.Pulse, FeatureId.FiveM, true)]
    [InlineData(PlanTier.Pulse, FeatureId.Aim, false)]
    [InlineData(PlanTier.Apex, FeatureId.Aim, true)]
    [InlineData(PlanTier.Apex, FeatureId.DisplayTuning, true)]
    public void FeatureCatalog_UsesCentralPlanHierarchy(PlanTier plan, FeatureId feature, bool expected)
        => Assert.Equal(expected, FeatureCatalog.IsAllowed(plan, feature));

    [Fact]
    public void FiveMValidation_RejectsWrongExecutableName()
    {
        var folder = Path.Combine(Path.GetTempPath(), "SecretFix.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var wrongName = Path.Combine(folder, "NotFiveM.exe");
            File.WriteAllBytes(wrongName, []);
            var validName = Path.Combine(folder, "FiveM.exe");
            File.WriteAllBytes(validName, []);

            Assert.False(FiveMService.IsValidExecutable(wrongName));
            Assert.True(FiveMService.IsValidExecutable(validName));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
