using System.Drawing;
using Forms = System.Windows.Forms;
using TurnSteamOn.Core;
using TurnSteamOn.Platform;

namespace TurnSteamOn;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
	private const string SingleInstanceName = "TurnSteamOn.App";
	private Forms.NotifyIcon? _trayIcon;
	private WindowsDualSenseConnectionMonitor? _controllerMonitor;
	private SteamStartupCoordinator? _steamCoordinator;
	private SingleInstanceGuard? _singleInstanceGuard;

	protected override void OnStartup(System.Windows.StartupEventArgs e)
	{
		base.OnStartup(e);
		ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

		_singleInstanceGuard = SingleInstanceGuard.TryAcquire(SingleInstanceName);
		if (_singleInstanceGuard is null)
		{
			Shutdown();
			return;
		}

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
		_steamCoordinator = new SteamStartupCoordinator(new WindowsSteamProcess());
		_controllerMonitor.DualSenseConnected += (_, _) =>
		{
			TemporaryLogger.Log("DualSenseConnected event received by the application.");
			_ = Dispatcher.BeginInvoke(() => statusItem.Text = "DualSense connected");
			_ = LaunchSteamAsync(statusItem);
		};

		try
		{
			_controllerMonitor.Start();
		}
		catch (Exception exception)
		{
			TemporaryLogger.Error("Unable to start the Bluetooth monitor.", exception);
			_ = Dispatcher.BeginInvoke(() => statusItem.Text = "Bluetooth monitor failed");
		}
	}

	private async Task LaunchSteamAsync(ToolStripItem statusItem)
	{
		try
		{
			if (await _steamCoordinator!.HandleDualSenseConnectedAsync())
			{
				_ = Dispatcher.BeginInvoke(() => statusItem.Text = "Steam launch requested");
			}
		}
		catch (Exception exception)
		{
			TemporaryLogger.Error("Unable to start Steam.", exception);
			_ = Dispatcher.BeginInvoke(() => statusItem.Text = "Steam launch failed");
		}
	}

	protected override void OnExit(System.Windows.ExitEventArgs e)
	{
		_controllerMonitor?.Dispose();
		_trayIcon?.Dispose();
		_singleInstanceGuard?.Dispose();
		base.OnExit(e);
	}
}

