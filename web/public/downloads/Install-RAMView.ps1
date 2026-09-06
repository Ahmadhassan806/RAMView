# RAM VIEW Windows Installer Script (§32)
param(
    [switch]$CreateDesktopShortcut,
    [switch]$StartOnLogin
)

$ErrorActionPreference = "Stop"

$AppName = "RAM VIEW"
$ExeSource = Join-Path $PSScriptRoot "..\dist\portable\RAMView.App.exe"
$InstallDir = Join-Path $env:LOCALAPPDATA "Programs\RAMView"
if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

$TargetExe = Join-Path $InstallDir "RAMView.exe"
if (Test-Path $ExeSource) {
    Copy-Item -Path $ExeSource -Destination $TargetExe -Force
    Unblock-File $TargetExe -ErrorAction SilentlyContinue
}

# Copy launcher script for Smart App Control policy compatibility
$LauncherSource = Join-Path $PSScriptRoot "..\RAMView.cmd"
$TargetCmd = Join-Path $InstallDir "RAMView.cmd"
if (Test-Path $LauncherSource) {
    Copy-Item -Path $LauncherSource -Destination $TargetCmd -Force
}

# Copy compiled binaries
$BinSource = Join-Path $PSScriptRoot "..\src\RAMView.App\bin\Release\net10.0-windows10.0.19041.0\win-x64"
if (Test-Path $BinSource) {
    Copy-Item -Path "$BinSource\*" -Destination $InstallDir -Recurse -Force -ErrorAction SilentlyContinue
}

# Create Start Menu Shortcut
$StartMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\RAM VIEW"
if (-not (Test-Path $StartMenuDir)) {
    New-Item -ItemType Directory -Path $StartMenuDir -Force | Out-Null
}
$WshShell = New-Object -ComObject WScript.Shell
$StartShortcut = $WshShell.CreateShortcut((Join-Path $StartMenuDir "RAM VIEW.lnk"))
$StartShortcut.TargetPath = if (Test-Path $TargetCmd) { $TargetCmd } else { $TargetExe }
$StartShortcut.WorkingDirectory = $InstallDir
$StartShortcut.Description = "RAM VIEW — Real-time Squarified Treemap RAM Visualizer"
$StartShortcut.Save()

if ($CreateDesktopShortcut) {
    $DesktopShortcut = $WshShell.CreateShortcut((Join-Path ([Environment]::GetFolderPath("Desktop")) "RAM VIEW.lnk"))
    $DesktopShortcut.TargetPath = if (Test-Path $TargetCmd) { $TargetCmd } else { $TargetExe }
    $DesktopShortcut.WorkingDirectory = $InstallDir
    $DesktopShortcut.Description = "RAM VIEW"
    $DesktopShortcut.Save()
    Write-Host "Created Desktop Shortcut." -ForegroundColor Green
}

if ($StartOnLogin) {
    $RunKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    Set-ItemProperty -Path $RunKey -Name "RAMView" -Value "`"$TargetCmd`""
    Write-Host "Configured Start with Windows." -ForegroundColor Green
}

# Register in Windows Add/Remove Programs (Clean Uninstall support §32)
$UninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\RAMView"
if (-not (Test-Path $UninstallKey)) {
    New-Item -Path $UninstallKey -Force | Out-Null
}
Set-ItemProperty -Path $UninstallKey -Name "DisplayName" -Value "RAM VIEW"
Set-ItemProperty -Path $UninstallKey -Name "DisplayVersion" -Value "1.0.0"
Set-ItemProperty -Path $UninstallKey -Name "Publisher" -Value "RAM View Team"
Set-ItemProperty -Path $UninstallKey -Name "DisplayIcon" -Value $TargetExe
Set-ItemProperty -Path $UninstallKey -Name "InstallLocation" -Value $InstallDir
$UninstallScript = Join-Path $InstallDir "Uninstall.ps1"
Copy-Item -Path (Join-Path $PSScriptRoot "Uninstall-RAMView.ps1") -Destination $UninstallScript -Force
Set-ItemProperty -Path $UninstallKey -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$UninstallScript`""

Write-Host "Successfully installed RAM VIEW 1.0.0!" -ForegroundColor Green
Write-Host "Installed to: $InstallDir" -ForegroundColor Cyan
