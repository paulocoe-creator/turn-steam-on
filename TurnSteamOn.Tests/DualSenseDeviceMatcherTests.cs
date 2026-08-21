using TurnSteamOn.Core;

namespace TurnSteamOn.Tests;

public sealed class DualSenseDeviceMatcherTests
{
    [Theory]
    [InlineData("Wireless Controller", "BTHENUM\\DEV_054C&VID_054C&PID_0CE6", true)]
    [InlineData("Wireless Controller", "BTHENUM\\DEV_054C&VID_054C&PID_0DF2", true)]
    [InlineData("DualSense Wireless Controller", "BTHENUM\\DEV_054C&VID_054C&PID_0CE6", true)]
    [InlineData("Xbox Wireless Controller", "BTHENUM\\VID_054C&PID_0CE6", false)]
    [InlineData("Wireless Controller", "USB\\VID_054C&PID_0CE6", false)]
    [InlineData("Wireless Controller", "BTHENUM\\VID_045E&PID_0CE6", false)]
    public void MatchesDualSenseIdentity(string name, string deviceInstanceId, bool expected)
    {
        Assert.Equal(expected, DualSenseDeviceMatcher.IsDualSense(name, deviceInstanceId));
    }

    [Theory]
    [InlineData("Wireless Controller", true)]
    [InlineData("DualSense Wireless Controller", true)]
    [InlineData("Xbox Wireless Controller", false)]
    public void MatchesKnownDualSenseNames(string name, bool expected)
    {
        Assert.Equal(expected, DualSenseDeviceMatcher.IsDualSenseName(name));
    }
}