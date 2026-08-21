using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using TurnSteamOn.Core;

namespace TurnSteamOn.Platform;

public sealed class WindowsDualSenseConnectionMonitor : IControllerConnectionMonitor, IDisposable
{
    private static readonly string[] RequestedProperties = [DualSenseDeviceMatcher.ConnectedProperty, DualSenseDeviceMatcher.DeviceInstanceIdProperty];

    private readonly object _gate = new();
    private readonly HashSet<string> _dualSenseDevices = [];
    private readonly HashSet<string> _connectedDevices = [];
    private DeviceWatcher? _watcher;

    public event EventHandler? DualSenseConnected;

    public void Start()
    {
        lock (_gate)
        {
            if (_watcher is not null)
            {
                return;
            }

            var watcher = DeviceInformation.CreateWatcher(
                BluetoothDevice.GetDeviceSelector(),
                RequestedProperties,
                DeviceInformationKind.AssociationEndpoint);

            watcher.Added += OnAdded;
            watcher.Updated += OnUpdated;
            watcher.Removed += OnRemoved;
            watcher.Start();
            _watcher = watcher;
        }
    }

    public void Stop()
    {
        DeviceWatcher? watcher;

        lock (_gate)
        {
            watcher = _watcher;
            _watcher = null;
            _dualSenseDevices.Clear();
            _connectedDevices.Clear();
        }

        if (watcher is not null && watcher.Status is not DeviceWatcherStatus.Stopped and not DeviceWatcherStatus.Aborted)
        {
            watcher.Stop();
        }

    }

    public void Dispose()
    {
        Stop();
    }

    private void OnAdded(DeviceWatcher _, DeviceInformation device)
    {
        var instanceId = GetProperty(device, DualSenseDeviceMatcher.DeviceInstanceIdProperty);
        var connected = GetProperty(device, DualSenseDeviceMatcher.ConnectedProperty);

        if (!DualSenseDeviceMatcher.IsDualSense(device))
        {
            return;
        }

        TemporaryLogger.Log($"DualSense device added: name='{device.Name}', id='{device.Id}', instanceId='{instanceId}', connected='{connected}'.");

        lock (_gate)
        {
            _dualSenseDevices.Add(device.Id);

            if (!DualSenseDeviceMatcher.IsConnectedDualSense(device))
            {
                return;
            }

            if (!_connectedDevices.Add(device.Id))
            {
                return;
            }
        }

        DualSenseConnected?.Invoke(this, EventArgs.Empty);
        TemporaryLogger.Log($"DualSense connected: id='{device.Id}'.");
    }

    private void OnUpdated(DeviceWatcher _, DeviceInformationUpdate update)
    {
        lock (_gate)
        {
            if (!_dualSenseDevices.Contains(update.Id))
            {
                return;
            }

            if (!DualSenseDeviceMatcher.IsConnectedDualSense(update))
            {
                _connectedDevices.Remove(update.Id);
                TemporaryLogger.Log($"DualSense disconnected: id='{update.Id}', connected='{GetProperty(update, DualSenseDeviceMatcher.ConnectedProperty)}'.");
                return;
            }

            if (!_connectedDevices.Add(update.Id))
            {
                return;
            }
        }

        DualSenseConnected?.Invoke(this, EventArgs.Empty);
        TemporaryLogger.Log($"DualSense connected: id='{update.Id}', connected='{GetProperty(update, DualSenseDeviceMatcher.ConnectedProperty)}'.");
    }

    private void OnRemoved(DeviceWatcher _, DeviceInformationUpdate update)
    {
        lock (_gate)
        {
            var wasTracked = _dualSenseDevices.Remove(update.Id);
            _connectedDevices.Remove(update.Id);

            if (wasTracked)
            {
                TemporaryLogger.Log($"DualSense device removed: id='{update.Id}'.");
            }
        }
    }

    private static object? GetProperty(DeviceInformation device, string propertyName)
    {
        return device.Properties.TryGetValue(propertyName, out var value) ? value : null;
    }

    private static object? GetProperty(DeviceInformationUpdate update, string propertyName)
    {
        return update.Properties.TryGetValue(propertyName, out var value) ? value : null;
    }
}