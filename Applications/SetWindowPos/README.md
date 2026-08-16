# SetWindowPosition

A small Windows app that watches for a process's main window and
moves/resizes it using the Win32 `SetWindowPos` API — the same mechanism
SRWE uses, but scriptable so it can be launched automatically.

Launch it once with your target window's process name and desired
position/size, and a normal window opens (Alt-Tab / taskbar visible)
showing the parameters you passed in, and checks every second for that
window. The moment it appears, it's positioned automatically and the
window's status panel turns green with a confirmation and a sound. If
the window later closes and reopens (e.g. the game is restarted), it's
repositioned again. Close the window to stop watching.

## Build

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```
cd SetWindowPosition
dotnet build -c Release
```

The compiled exe will be at:
```
bin\x64\Release\net8.0-windows\SetWindowPosition.exe
```

## Usage

```
SetWindowPosition.exe <processName> <x> <y> <width> <height> [options]
```

- `processName` — the .exe name, with or without the `.exe` extension (e.g. `eurotrucks2`)
- `x`, `y` — top-left corner position, in screen pixels (can be negative, e.g. to start on a monitor to the left of your primary)
- `width`, `height` — target window size in pixels

### Options

| Option | Description | Default |
|---|---|---|
| `--wait-seconds N` | Wait N seconds before starting to watch for the window (gives a slow-loading game time to finish launching) | 0 |
| `--borderless` | Strip the title bar/border before positioning | off |

Once started, the app polls for the window every 1 second for as long as
it's running — there's no retry limit; it just keeps watching.

### Example for your ETS2 triple-monitor setup

```
SetWindowPosition.exe eurotrucks2 -1080 0 6000 1920 --wait-seconds 15
```

This waits 15 seconds after you run it (to give ETS2 time to fully load
to its main window), then finds `eurotrucks2.exe`'s window and moves/resizes
it to X=-1080, Y=0, Width=6000, Height=1920 — spanning your left monitor,
center monitor, and right monitor as one window.

Adjust the `-1080` starting X coordinate and other values based on your
actual monitor arrangement in Windows Display Settings.

## Automating the whole launch

You can wrap this in a simple batch file so a single double-click launches
ETS2 and then applies the window position automatically:

```bat
@echo off
start "" "C:\Program Files (x86)\Steam\steamapps\common\Euro Truck Simulator 2\bin\win_x64\eurotrucks2.exe"
"C:\Path\To\SetWindowPosition.exe" eurotrucks2 -1080 0 6000 1920 --wait-seconds 15
```

Save this as `LaunchETS2Triple.bat` and run it instead of launching the
game directly from Steam. Adjust the game path and the wait time to suit
how long your system actually takes to reach the main menu (you may need
to increase `--wait-seconds` if 15 isn't quite enough).

## Notes

- Run as Administrator if the target game is also running elevated —
  Windows won't let a non-elevated process manipulate an elevated one's
  window.
- If the window still refuses to resize/reposition even though the
  handle is found, some games ignore `SetWindowPos` entirely for their
  main window; there's no way around that from outside the game's own
  code.
- Since it has no console window, launching it without enough arguments
  shows a usage message box instead of printing to the console.
