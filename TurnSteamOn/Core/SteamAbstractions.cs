namespace TurnSteamOn.Core;

public interface ISteamProcess
{
    bool IsRunning();

    Task LaunchAsync(CancellationToken cancellationToken);
}
