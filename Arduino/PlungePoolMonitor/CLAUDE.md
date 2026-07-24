# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Firmware for an ESP32-based plunge pool temperature monitor. It reads a DS18B20
(OneWire/DallasTemperature) temperature sensor, shows the reading on an SSD1309
128x64 SPI OLED (U8g2), serves a live-updating status page over HTTP, and
periodically pushes readings to a Cloudflare Worker endpoint. It supports
over-the-air (OTA) firmware updates.

This is a single Arduino sketch (`PlungePoolMonitor.ino`), not a multi-target
or library project. There is no `platformio.ini`, `sketch.json`, or committed
board FQBN — it's built/uploaded via the Arduino IDE (or an equivalent
Visual Micro / arduino-cli setup) targeting an ESP32 board.

## Build / upload

There is no test suite or build script in this repo. To compile or flash,
use the Arduino IDE (open `PlungePoolMonitor.ino`) or `arduino-cli`, e.g.:

```
arduino-cli compile --fqbn esp32:esp32:esp32 .
arduino-cli upload --fqbn esp32:esp32:esp32 -p <PORT> .
```

Required libraries: `U8g2lib`, `OneWire`, `DallasTemperature`, plus the
ESP32 core's `WiFi`, `WiFiClientSecure`, `HTTPClient`, `WebServer`, and
`ArduinoOTA`.

Once initial Wi-Fi provisioning has succeeded, subsequent uploads can go
over OTA (hostname `plunge-pool-monitor`, see `OTAUpdate.cpp`) instead of USB.

## Architecture

The `.ino` file is the orchestrator: `setup()` brings up the display, sensor,
Wi-Fi, time sync, OTA, and web server; `loop()` services OTA/web-server
requests every iteration and, once a second, refreshes brightness, takes a
temperature reading, opportunistically uploads it to the cloud, and redraws
the OLED. Everything else lives in small single-purpose modules with a
matching `.h`/`.cpp` pair, each wrapping one Arduino/ESP32 library or concern:

- `WifiConnection` — tries a hardcoded list of SSID/password pairs in order,
  showing connection progress on the display; returns the SSID it joined (or
  `"No conn"`).
- `TimeManager` — wraps `configTzTime`/NTP sync (UK GMT/BST timezone rule)
  and exposes `getCurrentTime()` for the rest of the app.
- `DisplayManager` — thin OO wrapper around the raw `U8G2` display object
  (font/print/contrast/power-save), so the rest of the code never touches
  `U8g2lib` directly.
- `Webpage` — owns the `WebServer` instance and renders the `/` status page
  (self-refreshing HTML) from the shared `averageTemp` global.
- `CloudUpload` — POSTs `{temperature, deviceTime}` as JSON to a Cloudflare
  Worker (`plungepoolreadings.colinnaylor.workers.dev`) with a bearer token.
- `OTAUpdate` — configures `ArduinoOTA` (hostname + password) and its
  callback logging.

State is shared via a handful of file-scope globals declared in the `.ino`
(`averageTemp`, `currentDisplayMode`, upload timers, etc.) and pulled into
other translation units with `extern` (see the top of `Webpage.cpp`) —
there's no central app-state struct, so if you add a new module that needs
the live temperature or clock, follow that same `extern` pattern rather than
introducing a new state-passing mechanism.

Key runtime behaviors to preserve when touching `loop()`:
- Temperature is smoothed with a simple exponential moving average
  (90% old / 10% new) in `updateTemperature()`, not a raw instantaneous read.
- Display brightness/power follow a fixed daily schedule (`Off` 00:00–08:00,
  `Evening` dim from 19:00, otherwise `Day`), driven off NTP time via
  `updateDisplayBrightness()` / `setDisplayMode()`, which only acts on mode
  *changes* to avoid redundant display writes.
- Cloud upload is throttled to once per `CLOUD_UPLOAD_INTERVAL` (5 minutes)
  and skipped until the first temperature reading has landed.

## Secrets

Wi-Fi credentials, the OTA password, and the cloud upload bearer token live
in `Secrets.h`, which is gitignored and never committed. `WifiConnection.cpp`,
`OTAUpdate.cpp`, and `CloudUpload.cpp` all `#include "Secrets.h"` and consume
`WIFI_SSIDS`/`WIFI_PASSWORDS`/`WIFI_NETWORK_COUNT`, `OTA_PASSWORD`, and
`CLOUD_UPLOAD_TOKEN` respectively. `Secrets.h.example` is the committed
template — copy it to `Secrets.h` and fill in real values before building.
A local `Secrets.h` with the previously-hardcoded values already exists in
this working copy; treat it as sensitive and don't paste its contents into
diffs, issues, or anywhere else outside the project.
