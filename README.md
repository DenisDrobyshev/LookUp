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

> Windows SmartScreen may warn about a new unsigned app the first time — click
> **More info → Run anyway**. The app is open source; you can also build it
> yourself (see below).

## Usage

| Action | How |
| --- | --- |
| **Capture text** | Press `Ctrl + Shift + D`, then drag a box over the text. |
| Cancel a capture | `Esc` or right-click while selecting. |
| Capture (no hotkey) | Double-click the tray icon. |
| Change OCR language | Tray menu → **OCR language**. |
| Run at startup | Tray menu → **Run at Windows startup**. |
| Quit | Tray menu → **Quit LookUp**. |

The default hotkey is **`Ctrl + Shift + D`** rather than `Win + Shift + S`,
because Windows reserves the latter for its own screenshot tool.

### First-time OCR note

Windows recognizes one language per capture. LookUp follows your Windows
display language by default; if you mostly grab English text on a non-English
system, open the tray menu → **OCR language** and pick it once.

To add a language pack: **Windows Settings → Time & Language → Language & region
→ (your language) → Language options → Optical character recognition**.

## Settings

Editable JSON at `%APPDATA%\LookUp\settings.json` (tray menu → **Edit settings…**):

```json
{
  "Hotkey": "Ctrl+Shift+D",
  "Language": "",
  "KeepLineBreaks": true
}
```

- **Hotkey** — any combo of `Ctrl` / `Alt` / `Shift` / `Win` plus a key, e.g.
  `Ctrl+Shift+2` or `Alt+Q`.
- **Language** — a BCP-47 tag like `en` or `ru`. Empty = follow Windows.
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

## По-русски

**LookUp** — лёгкая утилита в трее: нажимаешь горячую клавишу, выделяешь область
на экране (как в «Ножницах»), и вместо картинки в буфер обмена попадает
**распознанный текст**. Работает локально на встроенном OCR Windows 10/11 —
без интернета и аккаунтов, распознаёт русский, английский и другие
установленные языки. Горячая клавиша по умолчанию — `Ctrl + Shift + D`
(`Win + Shift + S` занята системными «Ножницами»). Язык распознавания
переключается в меню трея одним кликом.

## License

[MIT](LICENSE) © Denis Drobyshev
