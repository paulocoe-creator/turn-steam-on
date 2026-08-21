using TurnSteamOn.Core;

namespace TurnSteamOn.Tests;

public sealed class DeviceTriggerPolicyTests
{
    private const string DeviceId = "bluetooth:controller-1";

    private readonly DeviceTriggerPolicy _policy = new();

    [Fact]
    public void AllowsASelectedEnabledDeviceOnAConnectionTransition()
    {
        var change = CreateChange(DeviceConnectionState.Disconnected, DeviceConnectionState.Connected);
        var selections = new[] { CreateSelection(DeviceId, enabled: true) };

        var decision = _policy.Evaluate(change, selections);

        Assert.Equal(DeviceTriggerDecision.Eligible, decision);
    }

    [Theory]
    [InlineData(DeviceConnectionState.Unknown, DeviceConnectionState.Connected)]
    [InlineData(DeviceConnectionState.Connected, DeviceConnectionState.Connected)]
    [InlineData(DeviceConnectionState.Disconnected, DeviceConnectionState.Disconnected)]
    [InlineData(DeviceConnectionState.Connected, DeviceConnectionState.Disconnected)]
    public void RejectsEventsThatAreNotNewConnectionTransitions(
        DeviceConnectionState previousState,
        DeviceConnectionState currentState)
    {
        var change = CreateChange(previousState, currentState);
        var selections = new[] { CreateSelection(DeviceId, enabled: true) };

        var decision = _policy.Evaluate(change, selections);

        Assert.Equal(DeviceTriggerDecision.NotAConnectionTransition, decision);
    }

    [Fact]
    public void AllowsAnUnavailableDeviceWhenItReconnects()
    {
        var change = CreateChange(DeviceConnectionState.Unavailable, DeviceConnectionState.Connected);
        var selections = new[] { CreateSelection(DeviceId, enabled: true) };

        var decision = _policy.Evaluate(change, selections);

        Assert.Equal(DeviceTriggerDecision.Eligible, decision);
    }

    [Fact]
    public void RejectsAnUnsupportedDevice()
    {
        var change = CreateChange(
            DeviceConnectionState.Disconnected,
            DeviceConnectionState.Connected,
            isSupported: false);
        var selections = new[] { CreateSelection(DeviceId, enabled: true) };

        var decision = _policy.Evaluate(change, selections);

        Assert.Equal(DeviceTriggerDecision.UnsupportedDevice, decision);
    }

    [Fact]
    public void RejectsAnUnselectedDevice()
    {
        var change = CreateChange(DeviceConnectionState.Disconnected, DeviceConnectionState.Connected);
        var selections = new[] { CreateSelection("bluetooth:controller-2", enabled: true) };

        var decision = _policy.Evaluate(change, selections);

        Assert.Equal(DeviceTriggerDecision.NotSelected, decision);
    }

    [Fact]
    public void RejectsASelectedButDisabledDevice()
    {
        var change = CreateChange(DeviceConnectionState.Disconnected, DeviceConnectionState.Connected);
        var selections = new[] { CreateSelection(DeviceId, enabled: false) };

        var decision = _policy.Evaluate(change, selections);

        Assert.Equal(DeviceTriggerDecision.Disabled, decision);
    }

    [Fact]
    public void MatchesSelectionsByStableIdInsteadOfFriendlyName()
    {
        var device = new InputDevice(
            DeviceId,
            "Wireless Controller",
            DeviceTransport.BluetoothClassic,
            DeviceConnectionState.Connected,
            isSupported: true,
            vendorId: 0x054C,
            productId: 0x0CE6);
        var change = new DeviceConnectionChanged(device, DeviceConnectionState.Disconnected);
        var selections = new[]
        {
            new DeviceSelection("bluetooth:controller-2", enabled: true, "Wireless Controller")
        };

        var decision = _policy.Evaluate(change, selections);

        Assert.Equal(DeviceTriggerDecision.NotSelected, decision);
    }

    [Fact]
    public void IgnoresAChangedFriendlyNameWhenTheStableIdMatches()
    {
        var change = CreateChange(DeviceConnectionState.Disconnected, DeviceConnectionState.Connected);
        var selections = new[]
        {
            new DeviceSelection(DeviceId, enabled: true, "Previous controller name")
        };

        var decision = _policy.Evaluate(change, selections);

        Assert.Equal(DeviceTriggerDecision.Eligible, decision);
    }

    [Fact]
    public void RequiresAStableDeviceId()
    {
        var exception = Assert.Throws<ArgumentException>(() => new InputDevice(
            " ",
            "Controller",
            DeviceTransport.BluetoothClassic,
            DeviceConnectionState.Disconnected,
            isSupported: true));

        Assert.Equal("stableId", exception.ParamName);
    }

    private static DeviceConnectionChanged CreateChange(
        DeviceConnectionState previousState,
        DeviceConnectionState currentState,
        bool isSupported = true)
    {
        var device = new InputDevice(
            DeviceId,
            "Controller",
            DeviceTransport.BluetoothClassic,
            currentState,
            isSupported,
            vendorId: 0x054C,
            productId: 0x0CE6);

        return new DeviceConnectionChanged(device, previousState);
    }

    private static DeviceSelection CreateSelection(string stableId, bool enabled)
    {
        return new DeviceSelection(stableId, enabled, "Controller");
    }
}
