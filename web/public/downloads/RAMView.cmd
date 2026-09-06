@echo off
setlocal
title RAM VIEW
cd /d "%~dp0"

:: Launch using the Microsoft-signed .NET 10 host (fully compatible with Windows Smart App Control and Application Control policies)
if exist "src\RAMView.App\bin\Release\net10.0-windows10.0.19041.0\win-x64\RAMView.App.dll" (
    start "" "C:\dotnet10\dotnet.exe" "src\RAMView.App\bin\Release\net10.0-windows10.0.19041.0\win-x64\RAMView.App.dll"
    exit /b 0
)

if exist "src\RAMView.App\bin\Debug\net10.0-windows10.0.19041.0\RAMView.App.dll" (
    start "" "C:\dotnet10\dotnet.exe" "src\RAMView.App\bin\Debug\net10.0-windows10.0.19041.0\RAMView.App.dll"
    exit /b 0
)

if exist "dist\portable\RAMView.App.exe" (
    start "" "dist\portable\RAMView.App.exe"
    exit /b 0
)

"C:\dotnet10\dotnet.exe" run --project src\RAMView.App\RAMView.App.csproj
