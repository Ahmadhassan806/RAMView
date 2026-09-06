# RAM VIEW Windows Clean Uninstaller Script (§32)
$ErrorActionPreference = "SilentlyContinue"

Write-Host "Uninstalling RAM VIEW..." -ForegroundColor Cyan

# Stop any running instances
Get-Process -Name "RAMView", "RAMView.App" -ErrorAction SilentlyContinue | Stop-Process -Force

# Remove Startup Run entry
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "RAMView" -ErrorAction SilentlyContinue

# Remove Start Menu Shortcut
$StartMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\RAM VIEW"
Remove-Item -Path $StartMenuDir -Recurse -Force -ErrorAction SilentlyContinue

# Remove Desktop Shortcut
$DesktopLnk = Join-Path ([Environment]::GetFolderPath("Desktop")) "RAM VIEW.lnk"
Remove-Item -Path $DesktopLnk -Force -ErrorAction SilentlyContinue

# Remove Uninstall registry entry
Remove-Item -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\RAMView" -Recurse -Force -ErrorAction SilentlyContinue

# Schedule removal of installation folder
$InstallDir = Join-Path $env:LOCALAPPDATA "Programs\RAMView"
Start-Process -FilePath "cmd.exe" -ArgumentList "/c timeout /t 2 /nobreak & rmdir /s /q `"$InstallDir`"" -WindowStyle Hidden

Write-Host "RAM VIEW uninstalled cleanly." -ForegroundColor Green
