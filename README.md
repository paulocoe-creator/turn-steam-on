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

The application currently starts as a tray-only shell. Bluetooth device monitoring, Steam installation discovery, and process launching will be added behind the interfaces in `TurnSteamOn/Core`.