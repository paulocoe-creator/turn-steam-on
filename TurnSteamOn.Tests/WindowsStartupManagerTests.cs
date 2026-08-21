using TurnSteamOn.Platform;

namespace TurnSteamOn.Tests;

public sealed class WindowsStartupManagerTests
{
    [Fact]
    public void EnablingStartupWritesAQuotedExecutableCommand()
    {
        var store = new FakeStartupEntryStore();
        var manager = new WindowsStartupManager(store, @"C:\Program Files\TurnSteamOn\TurnSteamOn.exe");

        manager.SetEnabled(true);

        Assert.Equal("\"C:\\Program Files\\TurnSteamOn\\TurnSteamOn.exe\"", store.Value);
    }

    [Fact]
    public void DisablingStartupRemovesTheEntry()
    {
        var store = new FakeStartupEntryStore { Value = "existing" };
        var manager = new WindowsStartupManager(store, "TurnSteamOn.exe");

        manager.SetEnabled(false);

        Assert.True(store.WasDeleted);
        Assert.Null(store.Value);
    }

    [Fact]
    public void ReadsWhetherStartupIsEnabled()
    {
        var store = new FakeStartupEntryStore { Value = "\"TurnSteamOn.exe\"" };
        var manager = new WindowsStartupManager(store, "TurnSteamOn.exe");

        Assert.True(manager.IsEnabled);
    }

    private sealed class FakeStartupEntryStore : IStartupEntryStore
    {
        public string? Value { get; set; }
        public bool WasDeleted { get; private set; }

        public string? GetValue(string name) => Value;

        public void SetValue(string name, string value)
        {
            Value = value;
            WasDeleted = false;
        }

        public void DeleteValue(string name)
        {
            Value = null;
            WasDeleted = true;
        }
    }
}