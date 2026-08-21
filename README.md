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

The application currently starts as a tray-only shell and monitors Bluetooth DualSense connections. Steam installation discovery and process launching remain separate follow-up work behind the interfaces in `TurnSteamOn/Core`.

## Manual Bluetooth test

Run the app with `dotnet run --project TurnSteamOn`. Pair the controller in Windows Bluetooth settings while holding **PS + Create**, then disconnect and reconnect it. The tray status should change to `DualSense connected`.

Temporary diagnostics are written to `%LOCALAPPDATA%\TurnSteamOn\turn-steam-on.log`.