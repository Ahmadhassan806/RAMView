# RAM VIEW — Installation Guide

## 🖥️ System Requirements

- **OS**: Windows 10 (version 1809+) or Windows 11 (64-bit)
- **Architecture**: x64
- **Privileges**: Standard user privileges (Administrator not required; will monitor all accessible user and system memory)

---

## 💾 Installation Methods

### Method 1: Portable Executable (Zero Installation)
1. Navigate to the `dist\portable` folder.
2. Double-click `RAMView.App.exe`.
3. The RAM VIEW overlay HUD will launch immediately.

### Method 2: System Installation (Start Menu & Programs Integration)
Run the automated installer from PowerShell:
```powershell
powershell -ExecutionPolicy Bypass -File .\installer\Install-RAMView.ps1 -CreateDesktopShortcut -StartOnLogin
```

What this does:
1. Copies `RAMView.App.exe` to `%LocalAppData%\Programs\RAMView\RAMView.exe`.
2. Creates a Start Menu shortcut under `RAM VIEW`.
3. Creates a Desktop shortcut.
4. Registers the application in Windows **Installed Apps** (Add/Remove Programs).
5. Optionally adds startup entry in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

---

## 🗑️ Uninstallation

### Via Windows Settings
1. Open Windows Settings (`Win + I`) → **Apps** → **Installed Apps**.
2. Locate **RAM VIEW** and click **Uninstall**.

### Via PowerShell Uninstaller Script
```powershell
powershell -ExecutionPolicy Bypass -File .\installer\Uninstall-RAMView.ps1
```
This terminates running instances, deletes shortcuts, clears registry entries, and removes program files cleanly.
