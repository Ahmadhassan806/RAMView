# RAM VIEW 🪟📊

[![WinGet](https://img.shields.io/badge/WinGet-Ahmadhassan806.RAMView-blue.svg?style=flat-square&logo=windows)](https://github.com/microsoft/winget-pkgs/pull/430789)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-Support%20Developer-orange.svg?style=flat-square&logo=buy-me-a-coffee)](https://buymeacoffee.com/ahmadhassan806)

> **Real-Time Squarified Treemap RAM Visualizer for Windows Desktop**

**RAM VIEW** is a modern Windows desktop utility that visualizes live system RAM usage as small, translucent, floating boxes — one box per process (or process group), sized proportionally to that process's Working Set physical memory consumption, arranged via a **squarified treemap layout** (Bruls, Huizing, van Wijk).

It transforms task monitoring from sterile lists into a living, ambient data visualization with a dark glass aesthetic.

---

## ✨ Features

- **Living Data Visualization**: Visualizes physical RAM usage as clean, semi-transparent white boxes with subtle borders and smooth typography.
- **Squarified Treemap Algorithm**: Implements the Bruls, Huizing, and van Wijk layout algorithm to optimize aspect ratios and prevent narrow slivers.
- **Dual Display Modes**:
  - **Mode A (Floating HUD Overlay)**: Movable, resizable, always-on-top transparent HUD window.
  - **Mode B (Taskbar / Compact Strip)**: Slim docked horizontal strip of proportional process blocks.
- **Pure Win32 Interop Layer**:
  - `WS_EX_LAYERED` and `WS_EX_TRANSPARENT` for true click-through ghost mode.
  - `DwmExtendFrameIntoClientArea` and dark mode attributes for native acrylic/glass blur.
  - Hardware-accelerated GPU rendering with `PerMonitorV2` High-DPI support.
- **Anti-Dominance & Min Size Floor (§6)**:
  - Configurable minimum visual size floor (default: 20 MB) so small utilities don't vanish.
  - Anti-dominance capping (default: 60% max area) so heavy apps like Chrome don't dwarf all other processes.
- **Live Search & Filter Presets (§13)**:
  - Instant substring search that dims non-matching boxes without jostling layout positions.
  - Quick filters: **All**, **Applications**, **System**, **Highest RAM**.
- **Process Inspection & Hardened Safe Termination (§12, §23)**:
  - Click any box to inspect process PID, Working Set, path, and instance count.
  - Protected system process denylist (`System`, `Idle`, `csrss.exe`, `wininit.exe`, `services.exe`, `smss.exe`, `lsass.exe`, `dwm.exe`, PID 0/4) prevents accidental BSODs or corruption.
- **Offline & Private (§20)**:
  - 100% local execution. Zero telemetry, zero external network calls, zero accounts.
- **System Tray & Global Hotkey (§15, §16)**:
  - System tray icon with live RAM tooltips and quick actions.
  - Global toggle hotkey (`Ctrl+Shift+R`).

---

## 🚀 Quick Start

### Install via Windows Package Manager (WinGet)
Run in PowerShell or Command Prompt:
```powershell
winget install Ahmadhassan806.RAMView
```

### Download Portable Release
Download the latest self-contained standalone executable directly from [GitHub Releases](https://github.com/Ahmadhassan806/RAMView/releases/latest) without installing any runtimes.

### Local Installation Script
Run the PowerShell installer script:
```powershell
powershell -ExecutionPolicy Bypass -File .\installer\Install-RAMView.ps1 -CreateDesktopShortcut
```
This installs RAM VIEW into `%LocalAppData%\Programs\RAMView`, creates Start Menu and Desktop shortcuts, and registers in Windows Add/Remove Programs for clean uninstallation.

---

## 🛠️ Technology Stack

| Layer | Component |
|---|---|
| Target OS | Windows 10 (1809+) & Windows 11 (64-bit) |
| Runtime | .NET 10 (C# 13, Nullable enabled) |
| UI Framework | Windows Desktop WPF + DirectX Hardware Composition |
| Window Interop | Win32 User32, DwmApi, Shell32 (`WS_EX_LAYERED`, `WS_EX_TRANSPARENT`, DWM blur) |
| Layout Algorithm | Squarified Treemap (Bruls, Huizing, van Wijk) |
| Testing | xUnit 2.9 + Microsoft.NET.Test.Sdk |
| Packaging | Portable x64 Self-Contained Binary & Windows Installer |

---

## 📊 Exact Memory Calculation Methodology (§6)
RAM VIEW intentionally uses **Working Set** (`Process.WorkingSet64`) as its primary metric because the product goal is visualizing **actual physical RAM occupation** in hardware memory:
1. `totalTrackedMemory` is defined as the sum of Working Set across all enumerated processes for the given cycle.
2. `processPercentage = processMemory / totalTrackedMemory`.
3. The visualizer applies the **minimum size floor** (20 MB) and **anti-dominance rule** (60% max share) before running the squarified treemap solver.

---

## ☕ Support the Developer

If you enjoy using **RAM VIEW** and want to support ongoing maintenance, new features, and development, consider buying me a coffee!

<a href="https://buymeacoffee.com/ahmadhassan806" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" width="180"></a>

Every cup helps keep this project fast, modern, and open-source! ❤️

