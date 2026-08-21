using TurnSteamOn.Core;

namespace TurnSteamOn.Tests;

public sealed class DeviceTriggerOrchestratorTests
{
    private const string DeviceId = "bluetooth-classic:controller-1";

    [Fact]
    public async Task LoadsPreferencesBeforeStartingTheMonitor()
    {
        var preferencesStore = new FakePreferencesStore(SelectedPreferences());
        var monitor = new FakeMonitor(() => preferencesStore.LoadCount == 1);
        using var orchestrator = CreateOrchestrator(
            monitor,
            preferencesStore,
            new FakeSteamProcess(isRunning: false));

        await orchestrator.StartAsync();

        Assert.Equal(1, preferencesStore.LoadCount);
        Assert.Equal(1, monitor.StartCount);
    }

    [Fact]
    public async Task LaunchesSteamForASelectedSupportedConnectionTransition()
    {
        var monitor = new FakeMonitor();
        var steam = new FakeSteamProcess(isRunning: false);
        using var orchestrator = CreateOrchestrator(
            monitor,
            new FakePreferencesStore(SelectedPreferences()),
            steam);
        var processed = NextProcessed(orchestrator);
        await orchestrator.StartAsync();

        monitor.Raise(CreateChange(
            DeviceConnectionState.Disconnected,
            DeviceConnectionState.Connected));
        var result = await processed;

        Assert.Equal(DeviceTriggerDecision.Eligible, result.Decision);
        Assert.True(result.SteamLaunchRequested);
        Assert.Equal(1, steam.LaunchCount);
    }

    [Fact]
    public async Task ReportsEligibleWithoutLaunchingWhenSteamIsAlreadyRunning()
    {
        var monitor = new FakeMonitor();
        var steam = new FakeSteamProcess(isRunning: true);
        using var orchestrator = CreateOrchestrator(
            monitor,
            new FakePreferencesStore(SelectedPreferences()),
            steam);
        var processed = NextProcessed(orchestrator);
        await orchestrator.StartAsync();

        monitor.Raise(CreateChange(
            DeviceConnectionState.Disconnected,
            DeviceConnectionState.Connected));
        var result = await processed;

        Assert.Equal(DeviceTriggerDecision.Eligible, result.Decision);
        Assert.False(result.SteamLaunchRequested);
        Assert.Equal(0, steam.LaunchCount);
    }

    [Fact]
    public async Task AppliesChangedSelectionsWithoutRestartingTheMonitor()
    {
        const string replacementDeviceId = "bluetooth-low-energy:controller-2";
        var monitor = new FakeMonitor();
        var steam = new FakeSteamProcess(isRunning: false);
        var store = new FakePreferencesStore(SelectedPreferences());
        using var orchestrator = CreateOrchestrator(
            monitor,
            store,
            steam);
        await orchestrator.StartAsync();

        await orchestrator.SaveAndApplyPreferencesAsync(
            SelectedPreferences(stableId: replacementDeviceId));
        var originalProcessed = NextProcessed(orchestrator);
        monitor.Raise(CreateChange(
            DeviceConnectionState.Disconnected,
            DeviceConnectionState.Connected));
        var originalResult = await originalProcessed;
        var replacementProcessed = NextProcessed(orchestrator);
        monitor.Raise(CreateChange(
            DeviceConnectionState.Disconnected,
            DeviceConnectionState.Connected,
            stableId: replacementDeviceId));
        var replacementResult = await replacementProcessed;

        Assert.Equal(DeviceTriggerDecision.NotSelected, originalResult.Decision);
        Assert.Equal(DeviceTriggerDecision.Eligible, replacementResult.Decision);
        Assert.True(replacementResult.SteamLaunchRequested);
        Assert.Equal(1, monitor.StartCount);
        Assert.Equal(0, monitor.StopCount);
        Assert.Equal(1, store.SaveCount);
        Assert.Equal(1, steam.LaunchCount);
    }

    [Fact]
    public async Task KeepsTheActiveSelectionWhenSavingChangedPreferencesFails()
    {
        var monitor = new FakeMonitor();
        var steam = new FakeSteamProcess(isRunning: false);
        var store = new FakePreferencesStore(SelectedPreferences())
        {
            SaveException = new IOException("Disk unavailable")
        };
        using var orchestrator = CreateOrchestrator(monitor, store, steam);
        await orchestrator.StartAsync();

        await Assert.ThrowsAsync<IOException>(() => orchestrator.SaveAndApplyPreferencesAsync(
            SelectedPreferences(stableId: "bluetooth-low-energy:controller-2")));
        var processed = NextProcessed(orchestrator);
        monitor.Raise(CreateChange(
            DeviceConnectionState.Disconnected,
            DeviceConnectionState.Connected));
        var result = await processed;

        Assert.Equal(DeviceTriggerDecision.Eligible, result.Decision);
        Assert.True(result.SteamLaunchRequested);
        Assert.Equal(1, steam.LaunchCount);
    }

    [Theory]
    [InlineData(DeviceConnectionState.Connected, true, true, true, DeviceTriggerDecision.NotSelected)]
    [InlineData(DeviceConnectionState.Connected, true, false, false, DeviceTriggerDecision.Disabled)]
    [InlineData(DeviceConnectionState.Connected, false, true, false, DeviceTriggerDecision.UnsupportedDevice)]
    [InlineData(DeviceConnectionState.Disconnected, true, true, false, DeviceTriggerDecision.NotAConnectionTransition)]
    public async Task DoesNotLaunchForIneligibleDeviceEvents(
        DeviceConnectionState currentState,
        bool isSupported,
        bool enabled,
        bool omitSelection,
        DeviceTriggerDecision expectedDecision)
    {
        var monitor = new FakeMonitor();
        var steam = new FakeSteamProcess(isRunning: false);
        var preferences = omitSelection
            ? AppPreferences.Default
            : new AppPreferences(
                AppPreferences.CurrentSchemaVersion,
                AppTheme.System,
                [new DeviceSelection(DeviceId, enabled, "Controller")]);
        using var orchestrator = CreateOrchestrator(
            monitor,
            new FakePreferencesStore(preferences),
            steam);
        var processed = NextProcessed(orchestrator);
        await orchestrator.StartAsync();

        monitor.Raise(CreateChange(
            DeviceConnectionState.Disconnected,
            currentState,
            isSupported));
        var result = await processed;

        Assert.Equal(expectedDecision, result.Decision);
        Assert.False(result.SteamLaunchRequested);
        Assert.Equal(0, steam.LaunchCount);
    }

    [Fact]
    public async Task ReportsSteamLaunchFailuresWithoutLeakingAsyncEventExceptions()
    {
        var monitor = new FakeMonitor();
        using var orchestrator = CreateOrchestrator(
            monitor,
            new FakePreferencesStore(SelectedPreferences()),
            new ThrowingSteamProcess());
        var failed = NextFailure(orchestrator);
        await orchestrator.StartAsync();

        monitor.Raise(CreateChange(
            DeviceConnectionState.Disconnected,
            DeviceConnectionState.Connected));
        var failure = await failed;

        Assert.Equal(DeviceId, failure.Change.Device.StableId);
        Assert.IsType<InvalidOperationException>(failure.Exception);
    }

    [Fact]
    public async Task DoesNotStartMonitoringWhenPreferencesCannotBeLoaded()
    {
        var monitor = new FakeMonitor();
        var store = new FakePreferencesStore(new InvalidDataException("Invalid preferences"));
        using var orchestrator = CreateOrchestrator(
            monitor,
            store,
            new FakeSteamProcess(isRunning: false));

        await Assert.ThrowsAsync<InvalidDataException>(() => orchestrator.StartAsync());

        Assert.Equal(0, monitor.StartCount);
    }

    [Fact]
    public async Task UnsubscribesWhenMonitorStartupFails()
    {
        var monitor = new FakeMonitor { StartException = new InvalidOperationException("No Bluetooth") };
        var steam = new FakeSteamProcess(isRunning: false);
        using var orchestrator = CreateOrchestrator(
            monitor,
            new FakePreferencesStore(SelectedPreferences()),
            steam);

        await Assert.ThrowsAsync<InvalidOperationException>(() => orchestrator.StartAsync());
        monitor.Raise(CreateChange(
            DeviceConnectionState.Disconnected,
            DeviceConnectionState.Connected));
        await Task.Delay(50);

        Assert.Equal(0, steam.LaunchCount);
    }

    [Fact]
    public async Task StopUnsubscribesAndStopsTheMonitor()
    {
        var monitor = new FakeMonitor();
        var steam = new FakeSteamProcess(isRunning: false);
        using var orchestrator = CreateOrchestrator(
            monitor,
            new FakePreferencesStore(SelectedPreferences()),
            steam);
        await orchestrator.StartAsync();

        orchestrator.Stop();
        monitor.Raise(CreateChange(
            DeviceConnectionState.Disconnected,
            DeviceConnectionState.Connected));
        await Task.Delay(50);

        Assert.Equal(1, monitor.StopCount);
        Assert.Equal(0, steam.LaunchCount);
    }

    private static DeviceTriggerOrchestrator CreateOrchestrator(
        IInputDeviceConnectionMonitor monitor,
        IDevicePreferencesStore preferencesStore,
        ISteamProcess steamProcess)
    {
        return new DeviceTriggerOrchestrator(
            monitor,
            preferencesStore,
            new DeviceTriggerPolicy(),
            new SteamStartupCoordinator(steamProcess));
    }

    private static async Task<DeviceTriggerProcessed> NextProcessed(
        DeviceTriggerOrchestrator orchestrator)
    {
        var completion = new TaskCompletionSource<DeviceTriggerProcessed>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        orchestrator.TriggerProcessed += (_, result) => completion.TrySetResult(result);
        return await completion.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    private static async Task<DeviceTriggerFailed> NextFailure(
        DeviceTriggerOrchestrator orchestrator)
    {
        var completion = new TaskCompletionSource<DeviceTriggerFailed>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        orchestrator.TriggerFailed += (_, failure) => completion.TrySetResult(failure);
        return await completion.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    private static AppPreferences SelectedPreferences(
        bool enabled = true,
        string stableId = DeviceId)
    {
        return new AppPreferences(
            AppPreferences.CurrentSchemaVersion,
            AppTheme.System,
            [new DeviceSelection(stableId, enabled, "Controller")]);
    }

    private static DeviceConnectionChanged CreateChange(
        DeviceConnectionState previousState,
        DeviceConnectionState currentState,
        bool isSupported = true,
        string stableId = DeviceId)
    {
        return new DeviceConnectionChanged(
            new InputDevice(
                stableId,
                "Controller",
                DeviceTransport.BluetoothClassic,
                currentState,
                isSupported,
                vendorId: 0x054C,
                productId: 0x0CE6),
            previousState);
    }

    private sealed class FakeMonitor : IInputDeviceConnectionMonitor
    {
        private readonly Func<bool>? _startAssertion;

        public FakeMonitor(Func<bool>? startAssertion = null)
        {
            _startAssertion = startAssertion;
        }

        public event EventHandler<DeviceConnectionChanged>? ConnectionChanged;

        public Exception? StartException { get; init; }

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public void Start()
        {
            Assert.True(_startAssertion?.Invoke() ?? true);
            StartCount++;

            if (StartException is not null)
            {
                throw StartException;
            }
        }

        public void Stop() => StopCount++;

        public void Raise(DeviceConnectionChanged change) => ConnectionChanged?.Invoke(this, change);
    }

    private sealed class FakePreferencesStore : IDevicePreferencesStore
    {
        private readonly AppPreferences? _preferences;
        private readonly Exception? _loadException;

        public FakePreferencesStore(AppPreferences preferences)
        {
            _preferences = preferences;
        }

        public FakePreferencesStore(Exception loadException)
        {
            _loadException = loadException;
        }

        public int LoadCount { get; private set; }

        public int SaveCount { get; private set; }

        public Exception? SaveException { get; init; }

        public Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default)
        {
            LoadCount++;
            return _loadException is not null
                ? Task.FromException<AppPreferences>(_loadException)
                : Task.FromResult(_preferences!);
        }

        public Task SaveAsync(
            AppPreferences preferences,
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return SaveException is null
                ? Task.CompletedTask
                : Task.FromException(SaveException);
        }
    }

    private sealed class FakeSteamProcess : ISteamProcess
    {
        private readonly bool _isRunning;

        public FakeSteamProcess(bool isRunning)
        {
            _isRunning = isRunning;
        }

        public int LaunchCount { get; private set; }

        public bool IsRunning() => _isRunning;

        public Task LaunchAsync(CancellationToken cancellationToken)
        {
            LaunchCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingSteamProcess : ISteamProcess
    {
        public bool IsRunning() => false;

        public Task LaunchAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Steam failed");
        }
    }
}
