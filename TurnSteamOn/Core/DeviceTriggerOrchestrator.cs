namespace TurnSteamOn.Core;

public sealed class DeviceTriggerProcessed : EventArgs
{
    public DeviceTriggerProcessed(
        DeviceConnectionChanged change,
        DeviceTriggerDecision decision,
        bool steamLaunchRequested)
    {
        ArgumentNullException.ThrowIfNull(change);

        Change = change;
        Decision = decision;
        SteamLaunchRequested = steamLaunchRequested;
    }

    public DeviceConnectionChanged Change { get; }

    public DeviceTriggerDecision Decision { get; }

    public bool SteamLaunchRequested { get; }
}

public sealed class DeviceTriggerFailed : EventArgs
{
    public DeviceTriggerFailed(DeviceConnectionChanged change, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(change);
        ArgumentNullException.ThrowIfNull(exception);

        Change = change;
        Exception = exception;
    }

    public DeviceConnectionChanged Change { get; }

    public Exception Exception { get; }
}

public sealed class DeviceTriggerOrchestrator : IDisposable
{
    private readonly object _gate = new();
    private readonly IInputDeviceConnectionMonitor _monitor;
    private readonly IDevicePreferencesStore _preferencesStore;
    private readonly IDeviceTriggerPolicy _triggerPolicy;
    private readonly SteamStartupCoordinator _steamCoordinator;

    private AppPreferences? _preferences;
    private CancellationTokenSource? _lifetimeCancellation;
    private CancellationTokenSource? _startupCancellation;
    private bool _isStarting;
    private bool _isStarted;
    private bool _isDisposed;

    public DeviceTriggerOrchestrator(
        IInputDeviceConnectionMonitor monitor,
        IDevicePreferencesStore preferencesStore,
        IDeviceTriggerPolicy triggerPolicy,
        SteamStartupCoordinator steamCoordinator)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(preferencesStore);
        ArgumentNullException.ThrowIfNull(triggerPolicy);
        ArgumentNullException.ThrowIfNull(steamCoordinator);

        _monitor = monitor;
        _preferencesStore = preferencesStore;
        _triggerPolicy = triggerPolicy;
        _steamCoordinator = steamCoordinator;
    }

    public event EventHandler<DeviceTriggerProcessed>? TriggerProcessed;

    public event EventHandler<DeviceTriggerFailed>? TriggerFailed;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        CancellationTokenSource startupCancellation;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (_isStarted)
            {
                return;
            }

            if (_isStarting)
            {
                throw new InvalidOperationException("Device trigger monitoring is already starting.");
            }

            _isStarting = true;
            startupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _startupCancellation = startupCancellation;
        }

        try
        {
            var preferences = await _preferencesStore
                .LoadAsync(startupCancellation.Token)
                .ConfigureAwait(false);

            lock (_gate)
            {
                startupCancellation.Token.ThrowIfCancellationRequested();
                ObjectDisposedException.ThrowIf(_isDisposed, this);

                _preferences = preferences;
                _lifetimeCancellation = new CancellationTokenSource();
                _monitor.ConnectionChanged += OnConnectionChanged;

                try
                {
                    _monitor.Start();
                    _isStarted = true;
                }
                catch
                {
                    _monitor.ConnectionChanged -= OnConnectionChanged;
                    _lifetimeCancellation.Dispose();
                    _lifetimeCancellation = null;
                    _preferences = null;
                    throw;
                }
            }
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_startupCancellation, startupCancellation))
                {
                    _startupCancellation = null;
                }

                _isStarting = false;
            }

            startupCancellation.Dispose();
        }
    }

    public void Stop()
    {
        CancellationTokenSource? lifetimeCancellation;
        var shouldStopMonitor = false;

        lock (_gate)
        {
            _startupCancellation?.Cancel();

            if (!_isStarted)
            {
                return;
            }

            _isStarted = false;
            _monitor.ConnectionChanged -= OnConnectionChanged;
            shouldStopMonitor = true;
            lifetimeCancellation = _lifetimeCancellation;
            _lifetimeCancellation = null;
            _preferences = null;
        }

        lifetimeCancellation?.Cancel();

        try
        {
            if (shouldStopMonitor)
            {
                _monitor.Stop();
            }
        }
        finally
        {
            lifetimeCancellation?.Dispose();
        }
    }

    public async Task SaveAndApplyPreferencesAsync(
        AppPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (!_isStarted)
            {
                throw new InvalidOperationException("Device trigger monitoring is not running.");
            }

        }

        await _preferencesStore
            .SaveAsync(preferences, cancellationToken)
            .ConfigureAwait(false);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (!_isStarted)
            {
                throw new InvalidOperationException("Device trigger monitoring is not running.");
            }

            _preferences = preferences;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
        }

        Stop();
    }

    private async void OnConnectionChanged(object? sender, DeviceConnectionChanged change)
    {
        AppPreferences preferences;
        CancellationToken cancellationToken;

        lock (_gate)
        {
            if (!_isStarted || _preferences is null || _lifetimeCancellation is null)
            {
                return;
            }

            preferences = _preferences;
            cancellationToken = _lifetimeCancellation.Token;
        }

        try
        {
            var decision = _triggerPolicy.Evaluate(change, preferences.Devices);
            var launchRequested = decision == DeviceTriggerDecision.Eligible
                && await _steamCoordinator
                    .HandleDeviceConnectedAsync(cancellationToken)
                    .ConfigureAwait(false);

            TriggerProcessed?.Invoke(
                this,
                new DeviceTriggerProcessed(change, decision, launchRequested));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            TriggerFailed?.Invoke(this, new DeviceTriggerFailed(change, exception));
        }
    }
}
