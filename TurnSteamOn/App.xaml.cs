using System.Drawing;
using Forms = System.Windows.Forms;

namespace TurnSteamOn;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
	private Forms.NotifyIcon? _trayIcon;

	protected override void OnStartup(System.Windows.StartupEventArgs e)
	{
		base.OnStartup(e);
		ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

		var menu = new Forms.ContextMenuStrip();
		menu.Items.Add("Waiting for DualSense", null, null).Enabled = false;
		menu.Items.Add(new Forms.ToolStripSeparator());
		menu.Items.Add("Exit", null, (_, _) => Shutdown());

		_trayIcon = new Forms.NotifyIcon
		{
			Icon = SystemIcons.Application,
			Text = "Turn Steam On",
			ContextMenuStrip = menu,
			Visible = true
		};
	}

	protected override void OnExit(System.Windows.ExitEventArgs e)
	{
		_trayIcon?.Dispose();
		base.OnExit(e);
	}
}

