using System.Windows.Forms;
using TurnSteamOn.Platform;

namespace TurnSteamOn.Tests;

public sealed class TrayMenuControllerTests
{
    [Fact]
    public void CreatesTheExpectedBackgroundAppMenu()
    {
        var startup = new FakeStartupManager();
        var logOpened = false;
        var exited = false;
        using var controller = new TrayMenuController(
            startup,
            () => logOpened = true,
            () => exited = true);

        using var menu = controller.CreateMenu();

        Assert.Equal("Waiting for selected controller", controller.StatusText);
        Assert.NotNull(menu.Items["startup"]);
        Assert.NotNull(menu.Items["open-log"]);
        Assert.NotNull(menu.Items["exit"]);

        menu.Items["open-log"]!.PerformClick();
        menu.Items["exit"]!.PerformClick();

        Assert.True(logOpened);
        Assert.True(exited);
    }

    [Fact]
    public void StartupMenuItemReflectsAndChangesStartupState()
    {
        var startup = new FakeStartupManager { IsEnabled = true };
        using var controller = new TrayMenuController(startup, () => { }, () => { });
        using var menu = controller.CreateMenu();
        var startupItem = (ToolStripMenuItem)menu.Items["startup"]!;

        Assert.True(startupItem.Checked);

        startupItem.PerformClick();

        Assert.False(startup.IsEnabled);
        Assert.False(startupItem.Checked);
    }

    private sealed class FakeStartupManager : IStartupToggle
    {
        public bool IsEnabled { get; set; }

        public void SetEnabled(bool enabled) => IsEnabled = enabled;
    }
}
