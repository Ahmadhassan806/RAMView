# RAM VIEW — System Architecture & Design

## 🏛️ Layered Architectural Overview

RAM VIEW strictly adheres to MVVM and clean architecture separation. UI components never interact directly with Win32 APIs or operating system processes.

```
┌────────────────────────────────────────────────────────────┐
│                    RAMView.App (UI Layer)                  │
│   MainWindow, TreemapView, CompactStripView, Modals,       │
│   ValueConverters, XAML Resources & Glass Design System    │
└─────────────────────────────┬──────────────────────────────┘
                              │ MVVM Data Binding & Commands
┌─────────────────────────────▼──────────────────────────────┐
│                   RAMView.Core (Domain & MVVM)             │
│   - ViewModels: MainViewModel                              │
│   - Models: ProcessMemoryInfo, MemorySnapshot, TreemapRect │
│   - Algorithms: SquarifiedTreemap, MemoryCalculator        │
│   - Domain Interfaces: IMemoryMonitor, IProcessProvider,   │
│     ISystemMetricsProvider, ISettingsService, ITrayService │
└─────────────────────────────┬──────────────────────────────┘
                              │ Service Inversion
┌─────────────────────────────▼──────────────────────────────┐
│               RAMView.Infrastructure (Platform Layer)       │
│   - RAMMonitor: Win32 GlobalMemoryStatusEx query           │
│   - ProcessMonitor: Resilient Process enumeration & diff   │
│   - IconExtractor: Shell32 SHGetFileInfo & Appx manifests  │
│   - ProcessTerminator: Safe kill with Denylist protection  │
│   - HotkeyService: Win32 RegisterHotKey & message hook     │
│   - TrayService: Win32 NotifyIcon & tooltip throttling     │
│   - StartupManager: Windows Run key registration           │
└─────────────────────────────┬──────────────────────────────┘
                              │ P/Invoke & System Calls
┌─────────────────────────────▼──────────────────────────────┐
│                    Windows OS Kernel & APIs                │
│   kernel32.dll, user32.dll, dwmapi.dll, shell32.dll        │
└────────────────────────────────────────────────────────────┘
```

---

## 🧮 Algorithms & Domain Rules

### 1. Squarified Treemap Algorithm
Implemented based on Bruls, Huizing, and van Wijk (*Squarified Treemaps*, 2000):
- **Problem**: Naive slice-and-dice treemaps generate extreme aspect ratios (tall or wide slivers), making text and icons unreadable.
- **Solution**: The squarified algorithm processes items row by row along the shorter dimension of the available bounding box. Adding an item is accepted only if the worst aspect ratio $\max(w/h, h/w)$ improves or does not degrade.
- **Complexity**: $O(n \log n)$ due to initial weight sorting, followed by $O(n)$ row placement.

### 2. Anti-Dominance and Minimum Floor Rules (§6)
- **Minimum Floor**: Any process under `20 MB` is clamped up to `20 MB` for geometry weight calculations so it remains clickable and identifiable rather than shrinking to 0 px.
- **Anti-Dominance Capping**: If any single process exceeds `60%` of the total area, its weight is capped at 60%, and the remaining 40% area is distributed proportionally across all remaining processes.

### 3. Fault-Tolerant Process Enumeration (§22)
Querying Windows processes is subject to concurrent process exits and permission boundaries (e.g. system services, protected anticheat, other user sessions). Every call to `p.WorkingSet64`, `p.MainModule`, and `p.StartTime` is individually wrapped in resilient error handling. A failure for one process never aborts the polling cycle.

### 4. Critical Process Denylist (§12, §23)
To prevent destabilizing the operating system, `ProcessTerminator` verifies targets against a strict core denylist:
- PID 0 (`Idle`), PID 4 (`System`)
- `csrss.exe`, `wininit.exe`, `services.exe`, `smss.exe`, `lsass.exe`, `winlogon.exe`, `dwm.exe`, `fontdrvhost.exe`
Termination is blocked with an explanatory warning even if requested by the user.

---

## 🎨 Window Interop & Transparency Layer (§15)

WPF windows with `AllowsTransparency="True"` and `WindowStyle="None"` are integrated with the Windows Desktop Window Manager (DWM):
- `DwmExtendFrameIntoClientArea` with margins `(-1, -1, -1, -1)` enables client area hardware glass compositing.
- `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE)` forces Windows dark theme framing.
- `SetWindowLong(GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT)` dynamically toggles click-through mode.
- `SetWindowPos(HWND_TOPMOST)` maintains HUD pin status.
