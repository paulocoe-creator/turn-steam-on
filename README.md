# Turn Steam On

A Windows background utility that will start Steam when a PlayStation 5 DualSense controller connects over Bluetooth.

## Requirements

- Windows 10 or later
- .NET 10 SDK

## Build and test

```powershell
dotnet build TurnSteamOn.slnx
dotnet test TurnSteamOn.Tests/TurnSteamOn.Tests.csproj
```

The application starts as a tray-only background utility, monitors Bluetooth DualSense connections, and starts Steam when it is not already running.

## Manual Bluetooth test

Run the app with `dotnet run --project TurnSteamOn`. Pair the controller in Windows Bluetooth settings while holding **PS + Create**, then disconnect and reconnect it. The tray status should change to `DualSense connected`.

Temporary diagnostics are written to `%LOCALAPPDATA%\TurnSteamOn\turn-steam-on.log`.

## Build the installer

Install Inno Setup and make `iscc.exe` available on `PATH`, then run:

```powershell
.\Installer\Build-Installer.ps1
```

The script publishes a self-contained `win-x64` build, creates a Start menu shortcut named `Turn Steam On`, and registers the normal Windows uninstaller. The installer does not enable Windows startup automatically; that remains controlled by the tray menu setting.