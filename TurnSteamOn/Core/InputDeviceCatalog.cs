namespace TurnSteamOn.Core;

public sealed class InputDeviceCatalog : IInputDeviceCatalog
{
    private readonly object _gate = new();
    private readonly Dictionary<string, InputDevice> _devices = new(StringComparer.Ordinal);

    public event EventHandler<InputDeviceChanged>? DeviceChanged;

    public event EventHandler<DeviceConnectionChanged>? ConnectionChanged;

    public IReadOnlyList<InputDevice> GetDevices()
    {
        lock (_gate)
        {
            return _devices.Values
                .OrderBy(device => device.FriendlyName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(device => device.StableId, StringComparer.Ordinal)
                .ToArray();
        }
    }

    public void Upsert(InputDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        lock (_gate)
        {
            _devices.TryGetValue(device.StableId, out var previousDevice);
            if (device == previousDevice)
            {
                return;
            }

            _devices[device.StableId] = device;
            PublishChanges(device, previousDevice);
        }
    }

    public bool MarkUnavailable(string stableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stableId);

        InputDevice previousDevice;
        InputDevice unavailableDevice;

        lock (_gate)
        {
            if (!_devices.TryGetValue(stableId, out previousDevice!)
                || previousDevice.ConnectionState == DeviceConnectionState.Unavailable)
            {
                return false;
            }

            unavailableDevice = new InputDevice(
                previousDevice.StableId,
                previousDevice.FriendlyName,
                previousDevice.Transport,
                DeviceConnectionState.Unavailable,
                previousDevice.IsSupported,
                previousDevice.VendorId,
                previousDevice.ProductId);
            _devices[stableId] = unavailableDevice;
            PublishChanges(unavailableDevice, previousDevice);
        }

        return true;
    }

    public void Clear()
    {
        lock (_gate)
        {
            _devices.Clear();
        }
    }

    private void PublishChanges(InputDevice device, InputDevice? previousDevice)
    {
        DeviceChanged?.Invoke(this, new InputDeviceChanged(device, previousDevice));

        var previousState = previousDevice?.ConnectionState ?? DeviceConnectionState.Unavailable;
        if (previousState != device.ConnectionState)
        {
            ConnectionChanged?.Invoke(this, new DeviceConnectionChanged(device, previousState));
        }
    }
}
