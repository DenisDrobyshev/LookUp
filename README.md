<div align="center">

<img src="assets/logo.png" width="120" alt="LookUp logo" />

# LookUp

**Grab any text on your screen — like a screenshot, but you get the text.**

Press a hotkey, drag a box over anything on screen (a picture, a PDF, a video,
an error dialog you can't select), and the recognized text lands straight in
your clipboard. No cloud, no upload — recognition runs locally on Windows.

[![Release](https://img.shields.io/github/v/release/DenisDrobyshev/LookUp?include_prereleases&sort=semver)](https://github.com/DenisDrobyshev/LookUp/releases)
[![Build](https://github.com/DenisDrobyshev/LookUp/actions/workflows/release.yml/badge.svg)](https://github.com/DenisDrobyshev/LookUp/actions/workflows/release.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%20%2F%2011-0078D6)

**English** · [Русский](README.ru.md)

</div>

---

## Why

You can screenshot anything with `Win + Shift + S`, but then you're stuck with a
*picture*. Text inside images, scanned PDFs, videos, or apps that block copying
can't be selected. **LookUp** keeps that exact "select a region" flow — only
instead of an image, you get the **text**, ready to paste.

## Features

- 🖱️ **Snip-to-text** — the familiar drag-a-rectangle capture, across all monitors.
- ⚡ **Fast & local** — uses the OCR engine built into Windows 10/11. Nothing is
  sent anywhere; no API keys, no accounts.
- 🌍 **Multi-language** — recognizes any language you've installed OCR for
  (English, Russian, …). Switch in one click from the tray.
- 🪶 **Lightweight** — a single tray app. One global hotkey, out of your way.
- 📋 **Straight to clipboard** — capture, then `Ctrl + V` wherever you need it.
- 🔁 **Run at startup** — optional, toggled from the tray menu.

## Download

1. Go to the [**Releases**](https://github.com/DenisDrobyshev/LookUp/releases) page.
2. Download `LookUp-win-x64.zip` (self-contained — **no .NET install required**).
3. Unzip anywhere and run `LookUp.exe`. A small icon appears in the system tray.

### About the SmartScreen warning

Because LookUp is a new app that isn't code-signed yet, Windows SmartScreen may
show **"Windows protected your PC"** the first time you run it. That's expected
for any small independent tool — click **More info → Run anyway**.

You don't have to take that on faith:

- The full source is in this repo, and you can **build the exact same binary
  yourself** (see [Build from source](#build-from-source)).
- It runs **fully offline** — no network calls, no accounts.
- Every release ships a `SHA256SUMS.txt`. Confirm your download matches it:

  ```powershell
  Get-FileHash .\LookUp.exe -Algorithm SHA256
  ```

## Usage

| Action | How |
| --- | --- |
| **Capture text** | Press `Win + Shift + D`, then drag a box over the text. |
| Cancel a capture | `Esc` or right-click while selecting. |
| Capture (no hotkey) | Double-click the tray icon. |
| Change OCR language | Tray menu → **OCR language**. |
| Run at startup | Tray menu → **Run at Windows startup**. |
| Quit | Tray menu → **Quit LookUp**. |

The default hotkey is **`Win + Shift + D`** — the same gesture as Windows'
built-in `Win + Shift + S` screenshot tool, but the region comes back as text
instead of an image. If another app already owns the combo, LookUp falls back to
double-clicking the tray icon and you can set a different **Hotkey** in settings.

### First-time OCR note

Windows recognizes one language per capture. In **Auto** mode LookUp reads each
capture in the script of your **active keyboard layout** — so with the English
layout on, text made of letters shared by both alphabets (like `CAT` / `САТ`) is
read as Latin; switch to the Russian layout and the same shapes are read as
Cyrillic. Digits are identical either way. To lock one language regardless of
layout, pick it in the tray menu → **OCR language**. (Auto falls back to your
Windows display language if the current layout has no OCR recognizer installed.)

To add a language pack: **Windows Settings → Time & Language → Language & region
→ (your language) → Language options → Optical character recognition**.

## Settings

Editable JSON at `%APPDATA%\LookUp\settings.json` (tray menu → **Edit settings…**):

```json
{
  "Hotkey": "Win+Shift+D",
  "Language": "",
  "KeepLineBreaks": true
}
```

- **Hotkey** — any combo of `Ctrl` / `Alt` / `Shift` / `Win` plus a key, e.g.
  `Ctrl+Shift+2` or `Alt+Q`.
- **Language** — a BCP-47 tag like `en` or `ru` to pin one language. Empty =
  Auto (follow the active keyboard layout).
- **KeepLineBreaks** — keep line breaks (`true`) or join everything into one
  line (`false`).

## Build from source

Requires the [.NET SDK 9](https://dotnet.microsoft.com/download) on Windows 10/11.

```bash
git clone https://github.com/DenisDrobyshev/LookUp.git
cd LookUp
dotnet run --project src/LookUp/LookUp.csproj
```

Produce the same self-contained single file the releases ship:

```bash
dotnet publish src/LookUp/LookUp.csproj -c Release -r win-x64 ^
  --self-contained true -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

Sanity-check the OCR pipeline headlessly:

```bash
LookUp.exe --selftest
# writes result to %TEMP%\lookup-selftest.txt
```

## How it works

LookUp freezes the screen, lets you pick a region, crops that area, and runs it
through **`Windows.Media.Ocr`** — the on-device OCR engine shipped with Windows.
That's why it's small and quick and needs no bundled model data: it reuses the
language packs Windows already has. The OCR backend sits behind a small
`IOcrEngine` interface, so other engines can slot in later.

## Roadmap

- [ ] Cross-platform builds (macOS / Linux) via a Tesseract backend behind the
      same `IOcrEngine` interface.
- [ ] "Keep last N captures" history.
- [ ] Optional: paste recognized text directly instead of only copying.
- [ ] In-app hotkey picker (no need to edit the JSON).
- [ ] Per-monitor-DPI capture for pixel-perfect grabs on mixed-DPI setups.

Ideas and issues welcome.

## License

[MIT](LICENSE) © Denis Drobyshev
