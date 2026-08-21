# Turn Steam On

A Windows background utility that starts Steam when a supported, selected game controller connects over Bluetooth.

## Requirements

- Windows 10 or later
- .NET 10 SDK

## Build and test

```powershell
dotnet build TurnSteamOn.slnx
dotnet test TurnSteamOn.Tests/TurnSteamOn.Tests.csproj
```

The application starts as a tray-only background utility, monitors paired Bluetooth game controllers, and starts Steam when a selected controller connects and Steam is not already running.

## Manual Bluetooth test

Run the app with `dotnet run --project TurnSteamOn`, then disconnect and reconnect a paired Bluetooth game controller. Device discovery and trigger decisions are written to `%LOCALAPPDATA%\TurnSteamOn\turn-steam-on.log`.

Until the settings UI is available, no controller is selected implicitly, so an installation with no saved selections will monitor devices without launching Steam.

Temporary diagnostics are written to `%LOCALAPPDATA%\TurnSteamOn\turn-steam-on.log`.

## Build the installer

Install Inno Setup and make `iscc.exe` available on `PATH`, then run:

```powershell
.\Installer\Build-Installer.ps1
```

The script publishes a self-contained `win-x64` build, creates a Start menu shortcut named `Turn Steam On`, and registers the normal Windows uninstaller. The installer does not enable Windows startup automatically; that remains controlled by the tray menu setting.

Every push to `main` also runs the tests and builds a GitHub Release containing the versioned `TurnSteamOn-Setup-*.exe` installer. Users can download it from the repository's Releases page without cloning the project.

## Future improvements

Planned features include:

- A settings window that can be opened from the tray menu or Start menu.
- Device discovery with friendly names, connection state, Bluetooth or USB transport, vendor, product, and stable identifiers.
- Selecting one or more devices that can trigger Steam.
- Enabling or disabling selected devices independently.
- Persisting device selections and user preferences across restarts.
- Rescanning devices and testing which selected device would trigger Steam.
- Clear tray states for waiting, connected, launching, running, and error conditions.
- Pause and resume monitoring without exiting the application.
- Improved single-instance behavior that focuses the existing background app.
- Structured logging, log rotation, and clearer Steam launch diagnostics.
- Automated release builds, installer upgrades, and signed distribution artifacts.
- Optional per-device launch profiles, configurable delays, Big Picture mode, and specific game launching.
- Settings import/export, localization, and accessibility improvements.
