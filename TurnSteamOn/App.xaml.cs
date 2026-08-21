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
	private WindowsBluetoothDeviceCatalog? _deviceCatalog;
	private DeviceTriggerOrchestrator? _deviceTriggerOrchestrator;
	private SingleInstanceGuard? _singleInstanceGuard;
	private IStartupToggle? _startupManager;
	private TrayMenuController? _trayMenu;

	protected override async void OnStartup(System.Windows.StartupEventArgs e)
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

		_deviceCatalog = new WindowsBluetoothDeviceCatalog();
		_deviceTriggerOrchestrator = new DeviceTriggerOrchestrator(
			_deviceCatalog,
			new JsonDevicePreferencesStore(),
			new DeviceTriggerPolicy(),
			new SteamStartupCoordinator(new WindowsSteamProcess()));
		_deviceTriggerOrchestrator.TriggerProcessed += OnTriggerProcessed;
		_deviceTriggerOrchestrator.TriggerFailed += OnTriggerFailed;

		try
		{
			await _deviceTriggerOrchestrator.StartAsync();
		}
		catch (Exception exception)
		{
			TemporaryLogger.Error("Unable to start device trigger monitoring.", exception);
			_ = Dispatcher.BeginInvoke(() => _trayMenu.SetStatus("Monitoring failed"));
		}
	}

	private void OnTriggerProcessed(object? sender, DeviceTriggerProcessed result)
	{
		TemporaryLogger.Log(
			$"Device trigger evaluated: id='{result.Change.Device.StableId}', "
			+ $"name='{result.Change.Device.FriendlyName}', decision='{result.Decision}', "
			+ $"steamLaunchRequested='{result.SteamLaunchRequested}'.");

		if (result.Decision == DeviceTriggerDecision.Eligible)
		{
			var status = result.SteamLaunchRequested
				? "Steam launch requested"
				: "Steam already running";
			_ = Dispatcher.BeginInvoke(() => _trayMenu!.SetStatus(status));
		}
	}

	private void OnTriggerFailed(object? sender, DeviceTriggerFailed failure)
	{
		TemporaryLogger.Error(
			$"Unable to handle device trigger '{failure.Change.Device.StableId}'.",
			failure.Exception);
		_ = Dispatcher.BeginInvoke(() => _trayMenu!.SetStatus("Steam launch failed"));
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
		_deviceTriggerOrchestrator?.Dispose();
		_deviceCatalog?.Dispose();
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

