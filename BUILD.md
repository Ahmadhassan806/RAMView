# RAM VIEW — Build Instructions

## 📋 Prerequisites

- **Operating System**: Windows 10 (Build 1809+) or Windows 11 (x64)
- **.NET SDK**: .NET 10 SDK (`10.0.400` or newer) with Windows Desktop workload:
  ```powershell
  # Verify dotnet SDK
  dotnet --info
  ```

---

## 🔨 Compiling the Solution

From the root project directory:

### 1. Restore & Build (Debug)
```powershell
dotnet build RAMView.slnx
```

### 2. Run All Automated Unit Tests
```powershell
dotnet test tests\RAMView.Tests\RAMView.Tests.csproj
```

### 3. Build & Publish Standalone Portable Release Executable
To create a self-contained single-file executable requiring no preinstalled .NET runtimes:
```powershell
dotnet publish src\RAMView.App\RAMView.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o dist\portable
```

The resulting binary will be at:
`dist\portable\RAMView.App.exe`

---

## 📦 Building the Installer

### Option A: PowerShell Setup Script (No Compiler Required)
```powershell
powershell -ExecutionPolicy Bypass -File .\installer\Install-RAMView.ps1 -CreateDesktopShortcut
```

### Option B: Inno Setup Compiler (ISCC)
If Inno Setup is installed:
```cmd
iscc installer\RAMView_Setup.iss
```
The setup executable will be produced in `dist\installer\RAMView_Setup_x64.exe`.
