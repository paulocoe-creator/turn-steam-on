# Turn Steam On Technology Constitution

## Supported Platform

- Operating system: Windows 10 or later.
- Application runtime: .NET 10.
- Target framework: `net10.0-windows10.0.19041.0`.
- Application output: Windows GUI executable (`WinExe`).
- Distribution target: self-contained Windows x64 (`win-x64`).

The application is intentionally Windows-specific because its core behavior depends on Windows Bluetooth enumeration, the Windows Registry, process APIs, and the system tray.

## Desktop Application Stack

- WPF owns application startup, dispatcher access, lifetime, and explicit shutdown.
- Windows Forms supplies `NotifyIcon`, `ContextMenuStrip`, and tray menu controls.
- Windows Runtime APIs under `Windows.Devices.Bluetooth` and `Windows.Devices.Enumeration` supply event-driven Bluetooth device discovery and connection updates.
- `System.Diagnostics.Process` supplies Steam process detection and process launch.
- `Microsoft.Win32.Registry` supplies Steam installation discovery and the per-user Windows startup setting.

There is no required main window. The tray icon is the primary user interface.

## Source Boundaries

### Application composition

`TurnSteamOn/App.xaml.cs` is the composition root. It owns application lifecycle, creates concrete services, connects device events to Steam launch coordination, updates tray status, and disposes application-scoped resources.

### Core

`TurnSteamOn/Core` contains focused decision logic and narrow contracts:

- generic input-device catalog state, selection policy, and connection transitions;
- single-instance coordination;
- Steam startup serialization and decision-making; and
- input-device monitor, preferences-store, and Steam-process abstractions.

Core decision logic should remain directly testable. Windows API types may appear where the current device event boundary requires them, but operating-system side effects belong in platform implementations.

### Platform

`TurnSteamOn/Platform` contains Windows-facing implementations:

- Bluetooth Classic and Low Energy device watching and property mapping;
- tray menu construction;
- Windows startup registry access;
- Steam path resolution and process launch; and
- local diagnostic logging.

Platform behavior should be exposed through narrow interfaces or separable pure functions when it must be tested without real hardware, registry mutation, or process launch.

## State and Storage

The application has no database and no required network service.

Current persistent interactions are:

- `%LOCALAPPDATA%\TurnSteamOn\turn-steam-on.log` for diagnostics;
- `%LOCALAPPDATA%\TurnSteamOn\preferences.json` for versioned device selections and theme preferences;
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` for the opt-in startup entry; and
- Valve registry keys plus conventional Program Files locations for read-only Steam discovery.

User-controlled startup state must remain per-user. Logs and preferences must not require elevation.

## Dependencies

Production dependencies should remain minimal and Windows-native where practical.

The application project uses:

- the .NET Windows desktop SDK facilities for WPF and Windows Forms; and
- `Microsoft.Windows.SDK.NET.Ref` for Windows Runtime API references.

The test project uses:

- xUnit `2.9.3`;
- xUnit Visual Studio runner `3.1.4`;
- Microsoft.NET.Test.Sdk `17.14.1`; and
- Coverlet collector `6.0.4`.

Dependency additions must serve concrete product behavior or testability and must remain compatible with the supported Windows and .NET targets.

## Testing Standard

Behavior changes should be developed with focused automated tests. Tests should isolate hardware, registry, filesystem, shell, and process side effects behind collaborators or pure decision functions.

The established test suite covers:

- supported device identity and transport;
- generic input-device catalog state and Bluetooth property mapping;
- Steam-running and Steam-not-running decisions;
- concurrent connection events;
- single-instance acquisition;
- Windows startup commands;
- tray menu behavior;
- Steam executable candidate selection; and
- application metadata.

The standard local validation commands are:

```powershell
dotnet build TurnSteamOn.slnx
dotnet test TurnSteamOn.Tests/TurnSteamOn.Tests.csproj
```

## Packaging and Release

- `dotnet publish` produces a Release, self-contained `win-x64` application.
- Inno Setup 6 packages the publish output.
- The installer creates a Start menu shortcut and standard uninstall entry.
- Installation and normal application use target non-administrator operation.
- The installer must leave Windows startup disabled unless the user enables it from the tray menu.
- GitHub Actions on `main` runs tests, builds the installer, and publishes the versioned setup executable as a GitHub Release.

Application metadata, installer metadata, artifact names, and release tags must describe the same product version.

## Engineering Constraints

- Prefer event-driven device APIs over polling.
- Preserve cancellation, shutdown, and resource disposal paths.
- Treat duplicate and concurrent device events as normal operating conditions.
- Check Steam state immediately before launching it.
- Resolve Steam through standard installation evidence rather than a machine-specific path.
- Keep unrelated platform responsibilities separated and testable.
- Apply DRY, KISS, and SOLID pragmatically; abstractions must reduce real coupling or enable meaningful tests.
- Keep the application lightweight and avoid dependencies or background work that are not required by its mission.
