using System.Diagnostics;
using System.IO;
using TurnSteamOn.Core;

namespace TurnSteamOn.Platform;

public sealed class WindowsSteamProcess : ISteamProcess
{
    private readonly WindowsSteamInstallationResolver _installationResolver;

    public WindowsSteamProcess(WindowsSteamInstallationResolver? installationResolver = null)
    {
        _installationResolver = installationResolver ?? new WindowsSteamInstallationResolver();
    }

    public bool IsRunning()
    {
        return Process.GetProcessesByName("steam").Length > 0;
    }

    public Task LaunchAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var executablePath = _installationResolver.Resolve()
            ?? throw new FileNotFoundException("Steam could not be found. Install Steam or configure its installation path.");

        Process.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = true
        });

        return Task.CompletedTask;
    }
}