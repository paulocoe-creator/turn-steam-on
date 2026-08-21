# Turn Steam On Mission

## Purpose

Turn Steam On is a small Windows background utility that starts Steam when a supported PlayStation 5 DualSense controller connects over Bluetooth.

Its purpose is to remove a repetitive step from the start of a PC gaming session while remaining quiet, predictable, and under the user's control.

## Core Promise

When a qualifying DualSense Bluetooth connection is detected:

1. Determine whether Steam is already running.
2. Start Steam only when it is not running.
3. Report the outcome through the tray status and diagnostics.

Repeated or concurrent Windows device notifications must not cause duplicate Steam launches.

## Product Principles

### Precise triggering

Identify supported controllers using stable device evidence, including Bluetooth transport and known Sony vendor and product identifiers. A generic controller name alone is not sufficient when stronger device information is available.

### Quiet background operation

The application should have no required main window. It should remain lightweight while idle, prefer event-driven Windows APIs, and expose essential controls through the system tray.

### Safe, idempotent behavior

Connection events may be duplicated, reordered, or delivered concurrently. Handling them must be safe to repeat, and Steam must not be launched again when it is already running.

### User control

Users must be able to see the current status, choose whether the utility runs at Windows startup, open diagnostics, and exit the application explicitly. Installation must not silently enable startup behavior.

### Normal-user operation

Normal application behavior must not require administrator privileges. Machine-specific paths must not be hard-coded; Steam should be discovered from standard Windows installation information and conventional locations.

### Observable failure

Failures in device monitoring, Steam discovery, or process launch should be visible through status information and diagnostics rather than being silently ignored.

### Focused scope

Turn Steam On automates the transition from a supported controller connection to Steam startup. It does not replace Windows Bluetooth pairing, manage Steam itself, or treat every connected input device as a launch trigger.

## Success Criteria

The product fulfills its mission when it:

- runs as a single tray application on supported Windows systems;
- detects qualifying Bluetooth DualSense connection transitions;
- avoids false triggers from unrelated or USB devices;
- launches Steam at most once when needed;
- remains inactive when Steam is already running;
- shuts down cleanly and releases Windows resources; and
- can be installed, started, configured, diagnosed, and removed by a normal user.
