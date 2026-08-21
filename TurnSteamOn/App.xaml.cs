using System.Drawing;
using System.IO;
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
	private IStartupToggle? _startupManager;
	private TrayMenuController? _trayMenu;

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

		try
		{
			_startupManager = new WindowsStartupManager();
		}
		catch (Exception exception)
		{
			TemporaryLogger.Error("Unable to read Windows startup setting.", exception);
			_startupManager = new DisabledStartupToggle();
		}

		_trayMenu = new TrayMenuController(_startupManager, OpenLog, Shutdown);
		var menu = _trayMenu.CreateMenu();

		_trayIcon = new Forms.NotifyIcon
		{
			Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "favicon.ico")),
			Text = "Turn Steam On",
			ContextMenuStrip = menu,
			Visible = true
		};

		_controllerMonitor = new WindowsDualSenseConnectionMonitor();
		_steamCoordinator = new SteamStartupCoordinator(new WindowsSteamProcess());
		_controllerMonitor.DualSenseConnected += (_, _) =>
		{
			TemporaryLogger.Log("DualSenseConnected event received by the application.");
			_ = Dispatcher.BeginInvoke(() => _trayMenu.SetStatus("DualSense connected"));
			_ = LaunchSteamAsync();
		};

		try
		{
			_controllerMonitor.Start();
		}
		catch (Exception exception)
		{
			TemporaryLogger.Error("Unable to start the Bluetooth monitor.", exception);
			_ = Dispatcher.BeginInvoke(() => _trayMenu.SetStatus("Bluetooth monitor failed"));
		}
	}

	private async Task LaunchSteamAsync()
	{
		try
		{
			if (await _steamCoordinator!.HandleDeviceConnectedAsync())
			{
				_ = Dispatcher.BeginInvoke(() => _trayMenu!.SetStatus("Steam launch requested"));
			}
		}
		catch (Exception exception)
		{
			TemporaryLogger.Error("Unable to start Steam.", exception);
			_ = Dispatcher.BeginInvoke(() => _trayMenu!.SetStatus("Steam launch failed"));
		}
	}

	private static void OpenLog()
	{
		System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
		{
			FileName = TemporaryLogger.FilePath,
			UseShellExecute = true
		});
	}

	protected override void OnExit(System.Windows.ExitEventArgs e)
	{
		_controllerMonitor?.Dispose();
		_trayMenu?.Dispose();
		_trayIcon?.Dispose();
		_singleInstanceGuard?.Dispose();
		base.OnExit(e);
	}

	private sealed class DisabledStartupToggle : IStartupToggle
	{
		public bool IsEnabled => false;

		public void SetEnabled(bool enabled)
		{
			throw new InvalidOperationException("Windows startup configuration is unavailable.");
		}
	}
}

