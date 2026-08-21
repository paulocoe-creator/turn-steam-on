using TurnSteamOn.Core;

namespace TurnSteamOn.Tests;

public sealed class InputDeviceCatalogTests
{
    [Fact]
    public void AddsADeviceAndPublishesCatalogAndConnectionChanges()
    {
        var catalog = new InputDeviceCatalog();
        InputDeviceChanged? catalogChange = null;
        DeviceConnectionChanged? connectionChange = null;
        catalog.DeviceChanged += (_, change) => catalogChange = change;
        catalog.ConnectionChanged += (_, change) => connectionChange = change;
        var device = CreateDevice(
            "bluetooth-classic:controller-1",
            "Controller",
            DeviceConnectionState.Connected);

        catalog.Upsert(device);

        Assert.Null(catalogChange!.PreviousDevice);
        Assert.Equal(device, catalogChange.Device);
        Assert.Equal(DeviceConnectionState.Unavailable, connectionChange!.PreviousState);
        Assert.Equal(device, connectionChange.Device);
        Assert.True(connectionChange.IsConnectionTransition);
    }

    [Fact]
    public void PublishesMetadataChangesWithoutReportingAConnectionChange()
    {
        var catalog = new InputDeviceCatalog();
        var connectionChanges = 0;
        var catalogChanges = 0;
        catalog.ConnectionChanged += (_, _) => connectionChanges++;
        catalog.DeviceChanged += (_, _) => catalogChanges++;
        catalog.Upsert(CreateDevice(
            "bluetooth-classic:controller-1",
            "Old name",
            DeviceConnectionState.Disconnected));
        connectionChanges = 0;
        catalogChanges = 0;

        catalog.Upsert(CreateDevice(
            "bluetooth-classic:controller-1",
            "New name",
            DeviceConnectionState.Disconnected));

        Assert.Equal(1, catalogChanges);
        Assert.Equal(0, connectionChanges);
        Assert.Equal("New name", catalog.GetDevices().Single().FriendlyName);
    }

    [Fact]
    public void IgnoresAnIdenticalDeviceUpdate()
    {
        var catalog = new InputDeviceCatalog();
        var changes = 0;
        var device = CreateDevice(
            "bluetooth-classic:controller-1",
            "Controller",
            DeviceConnectionState.Connected);
        catalog.Upsert(device);
        catalog.DeviceChanged += (_, _) => changes++;
        catalog.ConnectionChanged += (_, _) => changes++;

        catalog.Upsert(device);

        Assert.Equal(0, changes);
    }

    [Fact]
    public void MarksRemovedDevicesUnavailableAndRetainsTheirMetadata()
    {
        var catalog = new InputDeviceCatalog();
        DeviceConnectionChanged? connectionChange = null;
        var device = CreateDevice(
            "bluetooth-low-energy:controller-1",
            "Controller",
            DeviceConnectionState.Connected,
            DeviceTransport.BluetoothLowEnergy);
        catalog.Upsert(device);
        catalog.ConnectionChanged += (_, change) => connectionChange = change;

        var changed = catalog.MarkUnavailable(device.StableId);

        var unavailable = catalog.GetDevices().Single();
        Assert.True(changed);
        Assert.Equal(DeviceConnectionState.Unavailable, unavailable.ConnectionState);
        Assert.Equal(device.FriendlyName, unavailable.FriendlyName);
        Assert.Equal(device.VendorId, unavailable.VendorId);
        Assert.Equal(DeviceConnectionState.Connected, connectionChange!.PreviousState);
        Assert.False(connectionChange.IsConnectionTransition);
    }

    [Fact]
    public void IgnoresRemovalOfAnUnknownDevice()
    {
        var catalog = new InputDeviceCatalog();
        var changes = 0;
        catalog.DeviceChanged += (_, _) => changes++;

        var changed = catalog.MarkUnavailable("bluetooth-classic:missing");

        Assert.False(changed);
        Assert.Equal(0, changes);
        Assert.Empty(catalog.GetDevices());
    }

    [Fact]
    public void ReturnsAnOrderedSnapshotThatCannotMutateCatalogState()
    {
        var catalog = new InputDeviceCatalog();
        catalog.Upsert(CreateDevice(
            "bluetooth-classic:controller-2",
            "Zulu Controller",
            DeviceConnectionState.Disconnected));
        catalog.Upsert(CreateDevice(
            "bluetooth-classic:controller-1",
            "Alpha Controller",
            DeviceConnectionState.Connected));

        var snapshot = catalog.GetDevices();
        ((InputDevice[])snapshot)[0] = CreateDevice(
            "bluetooth-classic:replacement",
            "Replacement",
            DeviceConnectionState.Connected);

        Assert.Equal(
            ["Alpha Controller", "Zulu Controller"],
            catalog.GetDevices().Select(device => device.FriendlyName));
    }

    private static InputDevice CreateDevice(
        string stableId,
        string friendlyName,
        DeviceConnectionState state,
        DeviceTransport transport = DeviceTransport.BluetoothClassic)
    {
        return new InputDevice(
            stableId,
            friendlyName,
            transport,
            state,
            isSupported: true,
            vendorId: 0x054C,
            productId: 0x0CE6);
    }
}
