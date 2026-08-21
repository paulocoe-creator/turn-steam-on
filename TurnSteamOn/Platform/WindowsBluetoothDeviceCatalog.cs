using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using TurnSteamOn.Core;

namespace TurnSteamOn.Platform;

public sealed class WindowsBluetoothDeviceCatalog :
    IInputDeviceCatalog,
    IInputDeviceConnectionMonitor,
    IDisposable
{
    private static readonly string[] RequestedProperties =
    [
        BluetoothInputDeviceMapper.CategoryProperty,
        BluetoothInputDeviceMapper.ClassOfDeviceMajorProperty,
        BluetoothInputDeviceMapper.ClassOfDeviceMinorProperty,
        BluetoothInputDeviceMapper.ConnectedProperty,
        BluetoothInputDeviceMapper.DeviceInstanceIdProperty,
        BluetoothInputDeviceMapper.LeAppearanceCategoryProperty,
        BluetoothInputDeviceMapper.LeAppearanceSubcategoryProperty
    ];

    private readonly object _gate = new();
    private readonly InputDeviceCatalog _catalog = new();
    private readonly Dictionary<DeviceWatcher, DeviceTransport> _watchers = [];
    private readonly Dictionary<string, DeviceInformation> _trackedDevices = new(StringComparer.Ordinal);

    public event EventHandler<InputDeviceChanged>? DeviceChanged
    {
        add => _catalog.DeviceChanged += value;
        remove => _catalog.DeviceChanged -= value;
    }

    public event EventHandler<DeviceConnectionChanged>? ConnectionChanged
    {
        add => _catalog.ConnectionChanged += value;
        remove => _catalog.ConnectionChanged -= value;
    }

    public IReadOnlyList<InputDevice> GetDevices() => _catalog.GetDevices();

    public void Start()
    {
        lock (_gate)
        {
            if (_watchers.Count > 0)
            {
                return;
            }

            AddWatcher(
                BluetoothDevice.GetDeviceSelectorFromPairingState(true),
                DeviceTransport.BluetoothClassic);
            AddWatcher(
                BluetoothLEDevice.GetDeviceSelectorFromPairingState(true),
                DeviceTransport.BluetoothLowEnergy);

            try
            {
                foreach (var watcher in _watchers.Keys)
                {
                    watcher.Start();
                }
            }
            catch
            {
                StopWatchers(_watchers.Keys.ToArray());
                _watchers.Clear();
                _trackedDevices.Clear();
                _catalog.Clear();
                throw;
            }
        }
    }

    public void Stop()
    {
        DeviceWatcher[] watchers;

        lock (_gate)
        {
            watchers = _watchers.Keys.ToArray();
            _watchers.Clear();
            _trackedDevices.Clear();
        }

        StopWatchers(watchers);
        _catalog.Clear();
    }

    public void Dispose()
    {
        Stop();
    }

    private void AddWatcher(string selector, DeviceTransport transport)
    {
        var watcher = DeviceInformation.CreateWatcher(
            selector,
            RequestedProperties,
            DeviceInformationKind.AssociationEndpoint);
        watcher.Added += OnAdded;
        watcher.Updated += OnUpdated;
        watcher.Removed += OnRemoved;
        _watchers.Add(watcher, transport);
    }

    private void OnAdded(DeviceWatcher watcher, DeviceInformation deviceInformation)
    {
        try
        {
            InputDevice device;

            lock (_gate)
            {
                if (!_watchers.TryGetValue(watcher, out var transport))
                {
                    return;
                }

                device = BluetoothInputDeviceMapper.Map(
                    deviceInformation.Id,
                    deviceInformation.Name,
                    transport,
                    deviceInformation.Properties);
                _trackedDevices[device.StableId] = deviceInformation;
            }

            _catalog.Upsert(device);
            LogDevice("added", device);
        }
        catch (Exception exception)
        {
            TemporaryLogger.Error("Unable to add a Bluetooth device to the catalog.", exception);
        }
    }

    private void OnUpdated(DeviceWatcher watcher, DeviceInformationUpdate update)
    {
        try
        {
            InputDevice device;

            lock (_gate)
            {
                if (!_watchers.TryGetValue(watcher, out var transport))
                {
                    return;
                }

                var stableId = BluetoothInputDeviceMapper.CreateStableId(update.Id, transport);
                if (!_trackedDevices.TryGetValue(stableId, out var deviceInformation))
                {
                    return;
                }

                deviceInformation.Update(update);
                device = BluetoothInputDeviceMapper.Map(
                    deviceInformation.Id,
                    deviceInformation.Name,
                    transport,
                    deviceInformation.Properties);
            }

            _catalog.Upsert(device);
            LogDevice("updated", device);
        }
        catch (Exception exception)
        {
            TemporaryLogger.Error("Unable to update a Bluetooth device in the catalog.", exception);
        }
    }

    private void OnRemoved(DeviceWatcher watcher, DeviceInformationUpdate update)
    {
        try
        {
            string stableId;

            lock (_gate)
            {
                if (!_watchers.TryGetValue(watcher, out var transport))
                {
                    return;
                }

                stableId = BluetoothInputDeviceMapper.CreateStableId(update.Id, transport);
                if (!_trackedDevices.Remove(stableId))
                {
                    return;
                }
            }

            if (_catalog.MarkUnavailable(stableId))
            {
                TemporaryLogger.Log($"Bluetooth device unavailable: id='{stableId}'.");
            }
        }
        catch (Exception exception)
        {
            TemporaryLogger.Error("Unable to remove a Bluetooth device from the catalog.", exception);
        }
    }

    private void StopWatchers(IEnumerable<DeviceWatcher> watchers)
    {
        foreach (var watcher in watchers)
        {
            watcher.Added -= OnAdded;
            watcher.Updated -= OnUpdated;
            watcher.Removed -= OnRemoved;

            if (watcher.Status is DeviceWatcherStatus.Started
                or DeviceWatcherStatus.EnumerationCompleted)
            {
                watcher.Stop();
            }
        }
    }

    private static void LogDevice(string action, InputDevice device)
    {
        TemporaryLogger.Log(
            $"Bluetooth device {action}: name='{device.FriendlyName}', id='{device.StableId}', "
            + $"transport='{device.Transport}', state='{device.ConnectionState}', supported='{device.IsSupported}', "
            + $"vendorId='{FormatHardwareId(device.VendorId)}', productId='{FormatHardwareId(device.ProductId)}'.");
    }

    private static string FormatHardwareId(ushort? value)
    {
        return value.HasValue ? $"{value.Value:X4}" : "unknown";
    }
}
