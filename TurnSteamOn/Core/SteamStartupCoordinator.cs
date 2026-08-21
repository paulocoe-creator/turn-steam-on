namespace TurnSteamOn.Core;

public sealed class SteamStartupCoordinator
{
    private readonly ISteamProcess _steamProcess;
    private readonly SemaphoreSlim _launchGate = new(1, 1);

    public SteamStartupCoordinator(ISteamProcess steamProcess)
    {
        _steamProcess = steamProcess;
    }

    public async Task<bool> HandleDualSenseConnectedAsync(CancellationToken cancellationToken = default)
    {
        await _launchGate.WaitAsync(cancellationToken);

        try
        {
            if (_steamProcess.IsRunning())
            {
                return false;
            }

            await _steamProcess.LaunchAsync(cancellationToken);
            return true;
        }
        finally
        {
            _launchGate.Release();
        }
    }
}