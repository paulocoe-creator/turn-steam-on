using TurnSteamOn.Core;

namespace TurnSteamOn.Tests;

public sealed class DevicePreferencesTests
{
    [Fact]
    public void DefaultsToTheCurrentSchemaSystemThemeAndNoDevices()
    {
        var preferences = AppPreferences.Default;

        Assert.Equal(AppPreferences.CurrentSchemaVersion, preferences.SchemaVersion);
        Assert.Equal(AppTheme.System, preferences.Theme);
        Assert.Empty(preferences.Devices);
    }

    [Fact]
    public void CopiesTheSuppliedDeviceSelections()
    {
        var selections = new List<DeviceSelection>
        {
            new("bluetooth:controller-1", enabled: true, "Controller 1")
        };

        var preferences = new AppPreferences(
            AppPreferences.CurrentSchemaVersion,
            AppTheme.Dark,
            selections);
        selections.Add(new DeviceSelection("bluetooth:controller-2", enabled: true, "Controller 2"));

        Assert.Single(preferences.Devices);
    }

    [Fact]
    public void RejectsDuplicateStableDeviceIds()
    {
        var selections = new[]
        {
            new DeviceSelection("bluetooth:controller-1", enabled: true, "Controller 1"),
            new DeviceSelection("bluetooth:controller-1", enabled: false, "Renamed controller")
        };

        var exception = Assert.Throws<ArgumentException>(() => new AppPreferences(
            AppPreferences.CurrentSchemaVersion,
            AppTheme.System,
            selections));

        Assert.Equal("devices", exception.ParamName);
    }

    [Fact]
    public void RejectsAnUnknownThemeValue()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new AppPreferences(
            AppPreferences.CurrentSchemaVersion,
            (AppTheme)999,
            []));

        Assert.Equal("theme", exception.ParamName);
    }
}
