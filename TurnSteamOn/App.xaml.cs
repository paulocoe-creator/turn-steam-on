using System.Drawing;
using Forms = System.Windows.Forms;
using TurnSteamOn.Platform;

namespace TurnSteamOn;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
	private Forms.NotifyIcon? _trayIcon;
	private WindowsDualSenseConnectionMonitor? _controllerMonitor;

	protected override void OnStartup(System.Windows.StartupEventArgs e)
	{
		base.OnStartup(e);
		ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

		var menu = new Forms.ContextMenuStrip();
		var statusItem = menu.Items.Add("Waiting for DualSense", null, null);
		statusItem.Enabled = false;
		menu.Items.Add(new Forms.ToolStripSeparator());
		menu.Items.Add("Exit", null, (_, _) => Shutdown());

		_trayIcon = new Forms.NotifyIcon
		{
			Icon = SystemIcons.Application,
			Text = "Turn Steam On",
			ContextMenuStrip = menu,
			Visible = true
		};

		_controllerMonitor = new WindowsDualSenseConnectionMonitor();
		_controllerMonitor.DualSenseConnected += (_, _) =>
		{
			TemporaryLogger.Log("DualSenseConnected event received by the application.");
			Dispatcher.BeginInvoke(() => statusItem.Text = "DualSense connected");
		};

		try
		{
			_controllerMonitor.Start();
		}
		catch (Exception exception)
		{
			TemporaryLogger.Error("Unable to start the Bluetooth monitor.", exception);
			Dispatcher.BeginInvoke(() => statusItem.Text = "Bluetooth monitor failed");
		}
	}

	protected override void OnExit(System.Windows.ExitEventArgs e)
	{
		_controllerMonitor?.Dispose();
		_trayIcon?.Dispose();
		base.OnExit(e);
	}
}

