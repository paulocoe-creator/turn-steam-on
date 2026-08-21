# Turn Steam On Roadmap

## Implemented

- Windows background utility with a system tray presence.
- Bluetooth monitoring for PlayStation 5 DualSense controllers.
- DualSense identification using Windows device information, Bluetooth transport, device names, and Sony identifiers when available.
- Reconnect handling and duplicate connection-event protection.
- Steam installation discovery through Windows registry locations and common installation paths.
- Steam process detection and launch when Steam is not already running.
- Concurrency protection so repeated controller events do not launch Steam more than once.
- Single-instance protection for the background app.
- Tray controls for status, opening logs, exiting, and enabling or disabling Windows startup.
- Application metadata and icon assets.
- Self-contained Windows publishing and an Inno Setup installer with Start menu integration and uninstallation support.
- Focused automated tests covering Bluetooth matching, Steam coordination, startup settings, tray behavior, metadata, and single-instance protection.

## Near-Term Improvements

### Reliability

- Improve Bluetooth device reconciliation when Windows reports transient connect/disconnect events.
- Add richer diagnostics for device properties and Steam launch results.
- Handle Steam startup failures and access-denied process states with clearer user feedback.
- Add tests for installer configuration and more Windows-specific failure cases.

### Background Experience

- Replace temporary file logging with configurable structured logging and log rotation.
- Add a clear tray status model for waiting, connected, launching, running, and error states.
- Add an option to pause or resume monitoring without exiting the application.
- Improve single-instance behavior so a second launch can bring attention to the existing tray application.

### Packaging and Distribution

- Add versioning automation for release builds.
- Add a GitHub Actions workflow to build, test, and produce the installer.
- Publish signed release artifacts when code-signing infrastructure is available.
- Add upgrade behavior that preserves user preferences and logs.

## Device Selection UI

The long-term goal is to let users choose which connected devices can trigger Steam instead of limiting the behavior to one hard-coded DualSense model.

### Device Configuration

- Enumerate supported Bluetooth and, where explicitly enabled, USB input devices.
- Show friendly names, connection state, transport, vendor, product, and stable device identifiers.
- Allow users to select one or more devices.
- Allow devices to be enabled or disabled independently.
- Persist selections using stable identifiers rather than friendly names alone.
- Handle renamed, re-paired, unavailable, and duplicate device entries gracefully.

### User Interface

- Add a small settings window that can be opened from the tray menu or the Start menu.
- Show a selectable device list with connection state and trigger status.
- Provide refresh and rescan actions.
- Provide a test action that reports which selected device would trigger Steam.
- Keep the app usable without the settings window; monitoring should continue in the background.
- Make the selected-device configuration accessible without administrator privileges.

### Architecture

- Replace the single `DualSenseConnected` event with a device connection event carrying a device model.
- Introduce a device catalog service separate from the Windows device watcher.
- Introduce a preferences store for selected device identifiers and user options.
- Keep device enumeration, selection policy, persistence, and UI behind separate interfaces so each part remains testable.
- Preserve the current default behavior by selecting the known DualSense device on first use when no preferences exist.

## Future Features

- Optional per-device Steam launch actions or profiles.
- Configurable delay before launching Steam after a connection.
- Optional behavior when all selected devices disconnect.
- Support for launching a specific Steam game or Big Picture mode.
- Import and export of user settings.
- Localization for the settings UI and installer.
- Accessibility improvements for keyboard navigation and screen readers.

## Guiding Principles

- Keep the app lightweight while idle.
- Prefer event-driven Windows APIs over polling.
- Keep platform-specific code at clear boundaries.
- Follow TDD for features, behavior changes, and bug fixes.
- Apply DRY, KISS, and SOLID pragmatically without adding abstractions that do not reduce real complexity.