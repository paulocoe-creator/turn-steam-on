using System.Windows.Forms;

namespace TurnSteamOn.Platform;

public sealed class TrayMenuController : IDisposable
{
    private readonly IStartupToggle _startupToggle;
    private readonly Action _openLog;
    private readonly Action _exit;
    private ToolStripMenuItem? _statusItem;

    public TrayMenuController(IStartupToggle startupToggle, Action openLog, Action exit)
    {
        _startupToggle = startupToggle;
        _openLog = openLog;
        _exit = exit;
    }

    public string StatusText => _statusItem?.Text ?? "Waiting for selected controller";

    public ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();
        _statusItem = new ToolStripMenuItem(StatusText)
        {
            Enabled = false
        };
        menu.Items.Add(_statusItem);

        menu.Items.Add(new ToolStripSeparator());

        var startupItem = new ToolStripMenuItem("Run at Windows startup")
        {
            Name = "startup",
            Checked = _startupToggle.IsEnabled
        };
        startupItem.Click += (_, _) =>
        {
            startupItem.Checked = !startupItem.Checked;
            _startupToggle.SetEnabled(startupItem.Checked);
        };
        menu.Items.Add(startupItem);

        menu.Items.Add(new ToolStripMenuItem("Open log")
        {
            Name = "open-log"
        });
        menu.Items["open-log"]!.Click += (_, _) => _openLog();

        menu.Items.Add(new ToolStripMenuItem("Exit")
        {
            Name = "exit"
        });
        menu.Items["exit"]!.Click += (_, _) => _exit();

        return menu;
    }

    public void SetStatus(string status)
    {
        if (_statusItem is not null)
        {
            _statusItem.Text = status;
        }
    }

    public void Dispose()
    {
        _statusItem = null;
    }
}
