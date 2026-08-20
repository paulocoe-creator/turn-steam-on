namespace TurnSteamOn.Core;

public sealed class SteamStartupCoordinator
{
    private readonly ISteamProcess _steamProcess;

    public SteamStartupCoordinator(ISteamProcess steamProcess)
    {
        _steamProcess = steamProcess;
    }

    public async Task<bool> HandleDualSenseConnectedAsync(CancellationToken cancellationToken = default)
    {
        if (_steamProcess.IsRunning())
        {
            return false;
        }

        await _steamProcess.LaunchAsync(cancellationToken);
        return true;
    }
}