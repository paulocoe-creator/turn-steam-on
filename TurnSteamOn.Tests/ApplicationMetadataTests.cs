using System.Reflection;
using TurnSteamOn;

namespace TurnSteamOn.Tests;

public sealed class ApplicationMetadataTests
{
    private static Assembly ApplicationAssembly => typeof(App).Assembly;

    [Fact]
    public void DefinesProductMetadata()
    {
        Assert.Equal("Turn Steam On", ApplicationAssembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product);
        Assert.Equal("Paulo Coelho", ApplicationAssembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company);
        Assert.Equal(
            "Starts Steam when a PS5 DualSense controller connects over Bluetooth.",
            ApplicationAssembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description);
    }

    [Fact]
    public void DefinesAnInformationalVersion()
    {
        var version = ApplicationAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        Assert.StartsWith("1.0.0", version);
    }
}