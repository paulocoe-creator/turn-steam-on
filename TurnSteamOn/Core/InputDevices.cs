namespace TurnSteamOn.Core;

public enum DeviceTransport
{
    Unknown,
    BluetoothClassic,
    BluetoothLowEnergy
}

public enum DeviceConnectionState
{
    Unknown,
    Disconnected,
    Connected,
    Unavailable
}

public sealed record InputDevice
{
    public InputDevice(
        string stableId,
        string friendlyName,
        DeviceTransport transport,
        DeviceConnectionState connectionState,
        bool isSupported,
        ushort? vendorId = null,
        ushort? productId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stableId);
        ArgumentException.ThrowIfNullOrWhiteSpace(friendlyName);

        StableId = stableId;
        FriendlyName = friendlyName;
        Transport = transport;
        ConnectionState = connectionState;
        IsSupported = isSupported;
        VendorId = vendorId;
        ProductId = productId;
    }

    public string StableId { get; }

    public string FriendlyName { get; }

    public DeviceTransport Transport { get; }

    public DeviceConnectionState ConnectionState { get; }

    public bool IsSupported { get; }

    public ushort? VendorId { get; }

    public ushort? ProductId { get; }
}

public sealed class DeviceConnectionChanged : EventArgs
{
    public DeviceConnectionChanged(InputDevice device, DeviceConnectionState previousState)
    {
        ArgumentNullException.ThrowIfNull(device);

        Device = device;
        PreviousState = previousState;
    }

    public InputDevice Device { get; }

    public DeviceConnectionState PreviousState { get; }

    public DeviceConnectionState CurrentState => Device.ConnectionState;

    public bool IsConnectionTransition =>
        PreviousState is DeviceConnectionState.Disconnected or DeviceConnectionState.Unavailable
        && CurrentState == DeviceConnectionState.Connected;
}

public sealed record DeviceSelection
{
    public DeviceSelection(string stableId, bool enabled, string lastKnownName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stableId);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastKnownName);

        StableId = stableId;
        Enabled = enabled;
        LastKnownName = lastKnownName;
    }

    public string StableId { get; }

    public bool Enabled { get; }

    public string LastKnownName { get; }
}

public interface IInputDeviceConnectionMonitor
{
    event EventHandler<DeviceConnectionChanged>? ConnectionChanged;

    void Start();

    void Stop();
}
