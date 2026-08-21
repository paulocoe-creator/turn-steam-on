using System.Globalization;
using Windows.Devices.Bluetooth;
using TurnSteamOn.Core;

namespace TurnSteamOn.Platform;

public static class BluetoothInputDeviceMapper
{
    public const string CategoryProperty = "System.Devices.Aep.Category";
    public const string ClassOfDeviceMajorProperty = "System.Devices.Aep.Bluetooth.Cod.Major";
    public const string ClassOfDeviceMinorProperty = "System.Devices.Aep.Bluetooth.Cod.Minor";
    public const string ConnectedProperty = "System.Devices.Aep.IsConnected";
    public const string DeviceInstanceIdProperty = "System.Devices.DeviceInstanceId";
    public const string LeAppearanceCategoryProperty = "System.Devices.Aep.Bluetooth.Le.Appearance.Category";
    public const string LeAppearanceSubcategoryProperty = "System.Devices.Aep.Bluetooth.Le.Appearance.Subcategory";

    private const string GamingCategory = "Input.Gaming";
    private const string UnknownDeviceName = "Unknown Bluetooth device";

    public static InputDevice Map(
        string endpointId,
        string? friendlyName,
        DeviceTransport transport,
        IReadOnlyDictionary<string, object> properties)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);
        ArgumentNullException.ThrowIfNull(properties);

        if (transport is not DeviceTransport.BluetoothClassic
            and not DeviceTransport.BluetoothLowEnergy)
        {
            throw new ArgumentOutOfRangeException(
                nameof(transport),
                transport,
                "A Bluetooth device requires a Bluetooth transport.");
        }

        var instanceId = GetString(properties, DeviceInstanceIdProperty);

        return new InputDevice(
            CreateStableId(endpointId, transport),
            string.IsNullOrWhiteSpace(friendlyName) ? UnknownDeviceName : friendlyName,
            transport,
            GetConnectionState(properties),
            IsSupportedGamingDevice(transport, properties),
            ParseHardwareId(instanceId, "VID_"),
            ParseHardwareId(instanceId, "PID_"));
    }

    public static string CreateStableId(string endpointId, DeviceTransport transport)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);

        var prefix = transport switch
        {
            DeviceTransport.BluetoothClassic => "bluetooth-classic:",
            DeviceTransport.BluetoothLowEnergy => "bluetooth-low-energy:",
            _ => throw new ArgumentOutOfRangeException(
                nameof(transport),
                transport,
                "A Bluetooth device requires a Bluetooth transport.")
        };

        return prefix + endpointId;
    }

    private static DeviceConnectionState GetConnectionState(
        IReadOnlyDictionary<string, object> properties)
    {
        return properties.TryGetValue(ConnectedProperty, out var value) && value is bool connected
            ? connected ? DeviceConnectionState.Connected : DeviceConnectionState.Disconnected
            : DeviceConnectionState.Unknown;
    }

    private static bool IsSupportedGamingDevice(
        DeviceTransport transport,
        IReadOnlyDictionary<string, object> properties)
    {
        if (properties.TryGetValue(CategoryProperty, out var categoriesValue)
            && categoriesValue is IEnumerable<string> categories
            && categories.Any(category => string.Equals(
                category,
                GamingCategory,
                StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return transport switch
        {
            DeviceTransport.BluetoothClassic => IsClassicGameController(properties),
            DeviceTransport.BluetoothLowEnergy => IsLowEnergyGameController(properties),
            _ => false
        };
    }

    private static bool IsClassicGameController(IReadOnlyDictionary<string, object> properties)
    {
        return TryGetUInt16(properties, ClassOfDeviceMajorProperty, out var majorClass)
            && majorClass == (ushort)BluetoothMajorClass.Peripheral
            && TryGetUInt16(properties, ClassOfDeviceMinorProperty, out var minorClass)
            && minorClass is (ushort)BluetoothMinorClass.PeripheralJoystick
                or (ushort)BluetoothMinorClass.PeripheralGamepad;
    }

    private static bool IsLowEnergyGameController(IReadOnlyDictionary<string, object> properties)
    {
        return TryGetUInt16(properties, LeAppearanceCategoryProperty, out var category)
            && category == BluetoothLEAppearanceCategories.HumanInterfaceDevice
            && TryGetUInt16(properties, LeAppearanceSubcategoryProperty, out var subcategory)
            && (subcategory == BluetoothLEAppearanceSubcategories.Joystick
                || subcategory == BluetoothLEAppearanceSubcategories.Gamepad);
    }

    private static bool TryGetUInt16(
        IReadOnlyDictionary<string, object> properties,
        string propertyName,
        out ushort result)
    {
        if (properties.TryGetValue(propertyName, out var value))
        {
            switch (value)
            {
                case ushort unsignedValue:
                    result = unsignedValue;
                    return true;
                case short signedValue when signedValue >= 0:
                    result = (ushort)signedValue;
                    return true;
                case uint unsignedValue when unsignedValue <= ushort.MaxValue:
                    result = (ushort)unsignedValue;
                    return true;
                case int signedValue when signedValue is >= 0 and <= ushort.MaxValue:
                    result = (ushort)signedValue;
                    return true;
            }
        }

        result = default;
        return false;
    }

    private static string? GetString(
        IReadOnlyDictionary<string, object> properties,
        string propertyName)
    {
        return properties.TryGetValue(propertyName, out var value) ? value as string : null;
    }

    private static ushort? ParseHardwareId(string? instanceId, string marker)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return null;
        }

        var markerIndex = instanceId.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0 || instanceId.Length < markerIndex + marker.Length + 4)
        {
            return null;
        }

        var value = instanceId.AsSpan(markerIndex + marker.Length, 4);
        return ushort.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}
