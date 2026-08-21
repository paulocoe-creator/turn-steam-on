using TurnSteamOn.Platform;

namespace TurnSteamOn.Tests;

public sealed class WindowsSteamInstallationResolverTests
{
    [Fact]
    public void ChoosesTheFirstExistingSteamExecutable()
    {
        var candidates = new[] { "missing-steam.exe", "installed-steam.exe", "later-steam.exe" };

        var executable = WindowsSteamInstallationResolver.FindExecutable(
            candidates,
            path => path == "installed-steam.exe");

        Assert.Equal("installed-steam.exe", executable);
    }

    [Fact]
    public void ReturnsNullWhenNoSteamExecutableExists()
    {
        var executable = WindowsSteamInstallationResolver.FindExecutable(
            ["missing-steam.exe"],
            _ => false);

        Assert.Null(executable);
    }
}