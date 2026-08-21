using TurnSteamOn.Core;
using TurnSteamOn.Platform;

namespace TurnSteamOn.Tests;

public sealed class BluetoothInputDeviceMapperTests
{
    [Fact]
    public void MapsAConnectedClassicGamepadWithoutUsingItsNameAsEvidence()
    {
        var properties = Properties(
            (BluetoothInputDeviceMapper.ConnectedProperty, true),
            (BluetoothInputDeviceMapper.ClassOfDeviceMajorProperty, (ushort)5),
            (BluetoothInputDeviceMapper.ClassOfDeviceMinorProperty, (ushort)2),
            (BluetoothInputDeviceMapper.DeviceInstanceIdProperty, "BTHENUM\\DEV_054C&VID_054C&PID_0CE6"));

        var device = BluetoothInputDeviceMapper.Map(
            "Bluetooth#Controller-1",
            "Living room controller",
            DeviceTransport.BluetoothClassic,
            properties);

        Assert.Equal("bluetooth-classic:Bluetooth#Controller-1", device.StableId);
        Assert.Equal("Living room controller", device.FriendlyName);
        Assert.Equal(DeviceTransport.BluetoothClassic, device.Transport);
        Assert.Equal(DeviceConnectionState.Connected, device.ConnectionState);
        Assert.True(device.IsSupported);
        Assert.Equal((ushort)0x054C, device.VendorId);
        Assert.Equal((ushort)0x0CE6, device.ProductId);
    }

    [Fact]
    public void MapsAClassicJoystickAsSupported()
    {
        var properties = Properties(
            (BluetoothInputDeviceMapper.ClassOfDeviceMajorProperty, (ushort)5),
            (BluetoothInputDeviceMapper.ClassOfDeviceMinorProperty, (ushort)1));

        var device = BluetoothInputDeviceMapper.Map(
            "Bluetooth#Joystick",
            "Flight stick",
            DeviceTransport.BluetoothClassic,
            properties);

        Assert.True(device.IsSupported);
    }

    [Fact]
    public void MapsBluetoothLeGamingAppearanceAsSupported()
    {
        var properties = Properties(
            (BluetoothInputDeviceMapper.ConnectedProperty, false),
            (BluetoothInputDeviceMapper.LeAppearanceCategoryProperty, (ushort)15),
            (BluetoothInputDeviceMapper.LeAppearanceSubcategoryProperty, (ushort)4));

        var device = BluetoothInputDeviceMapper.Map(
            "BluetoothLE#Controller-1",
            "BLE controller",
            DeviceTransport.BluetoothLowEnergy,
            properties);

        Assert.Equal("bluetooth-low-energy:BluetoothLE#Controller-1", device.StableId);
        Assert.Equal(DeviceConnectionState.Disconnected, device.ConnectionState);
        Assert.True(device.IsSupported);
    }

    [Fact]
    public void MapsTheWindowsGamingCategoryAsSupported()
    {
        var properties = Properties(
            (BluetoothInputDeviceMapper.CategoryProperty, new[] { "Audio.Headphone", "Input.Gaming" }));

        var device = BluetoothInputDeviceMapper.Map(
            "Bluetooth#CategorizedController",
            "Categorized controller",
            DeviceTransport.BluetoothClassic,
            properties);

        Assert.True(device.IsSupported);
    }

    [Fact]
    public void DoesNotTreatAControllerNameOrVendorIdAsSupportEvidence()
    {
        var properties = Properties(
            (BluetoothInputDeviceMapper.ConnectedProperty, true),
            (BluetoothInputDeviceMapper.DeviceInstanceIdProperty, "BTHENUM\\DEV_054C&VID_054C&PID_0CE6"));

        var device = BluetoothInputDeviceMapper.Map(
            "Bluetooth#UnclassifiedController",
            "Wireless Controller",
            DeviceTransport.BluetoothClassic,
            properties);

        Assert.False(device.IsSupported);
        Assert.Equal((ushort)0x054C, device.VendorId);
        Assert.Equal((ushort)0x0CE6, device.ProductId);
    }

    [Theory]
    [InlineData(true, DeviceConnectionState.Connected)]
    [InlineData(false, DeviceConnectionState.Disconnected)]
    [InlineData(null, DeviceConnectionState.Unknown)]
    public void MapsConnectionState(bool? connected, DeviceConnectionState expected)
    {
        var properties = connected.HasValue
            ? Properties((BluetoothInputDeviceMapper.ConnectedProperty, connected.Value))
            : Properties();

        var device = BluetoothInputDeviceMapper.Map(
            "Bluetooth#Controller",
            "Controller",
            DeviceTransport.BluetoothClassic,
            properties);

        Assert.Equal(expected, device.ConnectionState);
    }

    [Fact]
    public void UsesANeutralNameWhenWindowsDoesNotSupplyOne()
    {
        var device = BluetoothInputDeviceMapper.Map(
            "Bluetooth#Controller",
            " ",
            DeviceTransport.BluetoothClassic,
            Properties());

        Assert.Equal("Unknown Bluetooth device", device.FriendlyName);
    }

    [Fact]
    public void RejectsNonBluetoothTransports()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            BluetoothInputDeviceMapper.Map(
                "Device#1",
                "Controller",
                DeviceTransport.Unknown,
                Properties()));

        Assert.Equal("transport", exception.ParamName);
    }

    private static IReadOnlyDictionary<string, object> Properties(
        params (string Name, object Value)[] entries)
    {
        return entries.ToDictionary(entry => entry.Name, entry => entry.Value, StringComparer.Ordinal);
    }
}
