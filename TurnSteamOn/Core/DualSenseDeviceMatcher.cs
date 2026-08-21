using Windows.Devices.Enumeration;

namespace TurnSteamOn.Core;

public static class DualSenseDeviceMatcher
{
    public const string ConnectedProperty = "System.Devices.Aep.IsConnected";
    public const string DeviceInstanceIdProperty = "System.Devices.DeviceInstanceId";

    private static readonly string[] SupportedProductIds = ["0CE6", "0DF2"];

    public static bool IsConnectedDualSense(DeviceInformation device)
    {
        return IsDualSense(device)
            && IsConnected(device.Properties.TryGetValue(ConnectedProperty, out var connected) ? connected : null);
    }

    public static bool IsConnectedDualSense(DeviceInformationUpdate update)
    {
        return IsConnected(update.Properties.TryGetValue(ConnectedProperty, out var connected) ? connected : null);
    }

    public static bool IsDualSense(DeviceInformation device)
    {
        var deviceInstanceId = GetStringProperty(device, DeviceInstanceIdProperty);

        return IsDualSense(device.Name, deviceInstanceId)
            || (string.IsNullOrWhiteSpace(deviceInstanceId) && IsDualSenseName(device.Name));
    }

    public static bool IsDualSense(string? name, string? deviceInstanceId)
    {
        if (!IsDualSenseName(name)
            || !IsBluetoothDevice(deviceInstanceId))
        {
            return false;
        }

        return SupportedProductIds.Any(productId =>
            deviceInstanceId?.Contains($"VID_054C&PID_{productId}", StringComparison.OrdinalIgnoreCase) == true);
    }

    public static bool IsDualSenseName(string? name)
    {
        return string.Equals(name, "Wireless Controller", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "DualSense Wireless Controller", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsConnected(object? value) => value is bool connected && connected;

    private static bool IsBluetoothDevice(string? deviceInstanceId)
    {
        return deviceInstanceId?.Contains("BTHENUM\\", StringComparison.OrdinalIgnoreCase) == true
            || deviceInstanceId?.Contains("BTHLEDEVICE\\", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string? GetStringProperty(DeviceInformation device, string propertyName)
    {
        return device.Properties.TryGetValue(propertyName, out var value) ? value as string : null;
    }
}