# MASA Clipboard Manager

A lightweight, privacy-focused Windows clipboard manager built with **C# (.NET 8)**, **WPF**, and **SQLite**.

---

## Overview

MASA Clipboard Manager runs in the background and keeps track of copied text, rich formatting, HTML, images, and file paths without polling or excessive CPU usage. It integrates with Windows DPAPI to encrypt stored clipboard entries locally and detects sensitive credentials to prevent accidental recording of secrets.

---

## Screenshots

| Main Dashboard | Quick Paste Overlay (`Ctrl+Shift+V`) |
| :---: | :---: |
| ![Main Dashboard](Screenshots/01_Main_Dashboard.png) | ![Quick Paste](Screenshots/02_Quick_Paste_Overlay.png) |

| Developer Utilities Workbench | Settings & Privacy |
| :---: | :---: |
| ![DevTools](Screenshots/03_Developer_Tools.png) | ![Settings](Screenshots/04_Settings_and_Security.png) |

---

## Key Features

- **Clipboard Monitoring (Zero-Polling)**: Uses Win32 `AddClipboardFormatListener` / `WM_CLIPBOARDUPDATE` for event-driven clipboard capture with minimal system footprint.
- **Privacy & Sensitive Data Protection**:
  - Automatically identifies API keys (GitHub, OpenAI, AWS, Stripe, Google AI, Slack), JWT tokens, passwords, and credit card numbers (with Luhn validation).
  - Configurable privacy mode and automatic deletion for temporary credentials.
  - User-scoped local payload encryption via Windows DPAPI.
- **Quick Paste Overlay**:
  - Global hotkey (`Ctrl+Shift+V` by default) opens a compact, floating search window at the cursor.
  - Press `1-9` to paste directly or navigate with arrow keys and `Enter`.
- **Developer Tools**:
  - JSON formatting & minification.
  - Base64, URL, and HTML entity encoding / decoding.
  - Line operations: deduplication, sorting, trimming, and casing transforms.
- **Snippet & Text Editor**: In-app editor for modifying snippets before copying or pasting.
- **Organization & Retention**:
  - Custom colored collections (Work, Development, Personal, Templates).
  - Pinned and favorite items protected from auto-cleanup.
  - Application exclusions to ignore clipboard updates from specific processes (e.g., password managers).
- **System Tray Integration**: Minimizes cleanly to tray with pause/resume controls.

---

## Project Structure

```text
MASA.ClipboardManager/
│
├── MASA.ClipboardManager.Core/           # Domain models, enums, and interfaces
├── MASA.ClipboardManager.Application/    # Heuristics detectors, developer utilities, and business services
├── MASA.ClipboardManager.Infrastructure/ # Win32 native hooks, DPAPI, SQLite repositories, and safe logger
├── MASA.ClipboardManager.UI/             # Modern WPF views, view models, and dark themes
├── MASA.ClipboardManager.Tests/          # Unit test suite (xUnit)
└── Screenshots/                          # Real application captures
```

---

## Requirements

- **Operating System**: Windows 10 (Build 19041+) or Windows 11
- **Runtime**: [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Building and Running

### Clone the repository
```bash
git clone https://github.com/MASA/ClipboardManager.git
cd ClipboardManager
```

### Build the solution
```bash
dotnet build MASA.ClipboardManager.sln
```

### Run tests
```bash
dotnet test MASA.ClipboardManager.Tests\MASA.ClipboardManager.Tests.csproj
```

### Run the application
```bash
dotnet run --project MASA.ClipboardManager.UI\MASA.ClipboardManager.UI.csproj
```

---

## License

This project is licensed under the [MIT License](LICENSE) &copy; 2026 XREFS0.
>>>>>>> 8766309 (feat: initial commit of MASA Clipboard Manager (.NET 8, WPF, MVVM, SQLite, DPAPI))

---

### 🌐 Connect with Me

<p align="left">
  <a href="http://xrefs0.com/" target="_blank">
    <img src="https://img.icons8.com/bubbles/60/000000/domain.png" title="Website" width="45" height="45"/>
  </a>
  <a href="https://www.facebook.com/XREFS0" target="_blank">
    <img src="https://img.icons8.com/bubbles/60/000000/facebook-new.png" title="Facebook Page" width="45" height="45"/>
  </a>
  <a href="https://t.me/MrMasaOfficial" target="_blank">
    <img src="https://img.icons8.com/bubbles/60/000000/telegram-app.png" title="Telegram Contact" width="45" height="45"/>
  </a>
  <a href="https://t.me/XREFS0_CHANNEL" target="_blank">
    <img src="https://img.icons8.com/bubbles/60/000000/telegram-app.png" title="Telegram Channel" width="45" height="45"/>
  </a>
  <a href="https://www.youtube.com/@XREFS0" target="_blank">
    <img src="https://img.icons8.com/bubbles/60/000000/youtube-play.png" title="YouTube" width="45" height="45"/>
  </a>
</p>

