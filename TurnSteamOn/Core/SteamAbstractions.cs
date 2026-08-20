namespace TurnSteamOn.Core;

public interface IControllerConnectionMonitor
{
    event EventHandler? DualSenseConnected;

    void Start();

    void Stop();
}

public interface ISteamProcess
{
    bool IsRunning();

    Task LaunchAsync(CancellationToken cancellationToken);
}