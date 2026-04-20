# DevLauncher — WPF Edition

A native Windows desktop app built with **.NET 8 WPF** that lets IT office workers
launch their entire dev environment with a single click. No browser, no sandbox — it
actually runs your terminal commands using real OS processes.

---

## Project Structure

```
DevLauncher/
├── DevLauncher.csproj          # SDK-style project file (.NET 8 WPF)
├── App.xaml / App.xaml.cs      # Application entry point
├── Themes/
│   └── DarkTheme.xaml          # All colors, brushes, control styles
├── Models/
│   ├── LaunchCommand.cs        # A single terminal command
│   ├── LaunchPreset.cs         # A named group of commands
│   ├── TerminalLine.cs         # One line of terminal output
│   └── RunningProcess.cs       # Tracks a live OS process
├── Services/
│   ├── ProcessExecutionService.cs   # REAL process spawning + output streaming
│   └── PresetStorageService.cs      # JSON save/load + built-in presets
├── ViewModels/
│   └── MainViewModel.cs        # MVVM brain (CommunityToolkit.Mvvm)
├── Views/
│   ├── MainWindow.xaml         # Full dark UI
│   └── MainWindow.xaml.cs      # Code-behind (window chrome only)
└── Converters/
    ├── BoolToVisibilityConverter.cs
    ├── TerminalTypeToColorConverter.cs
    └── CategoryToBadgeColorConverter.cs
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | **8.0+** |
| Windows | 10 / 11 |
| Visual Studio | 2022 (recommended) OR `dotnet` CLI |

---

## Build & Run

### Option A — Visual Studio 2022
1. Open `DevLauncher.sln` (or just open the `.csproj`)
2. Press **F5** to build and run

### Option B — dotnet CLI
```bash
cd DevLauncher
dotnet restore
dotnet run
```

### Option C — Publish as single EXE
```bash
dotnet publish -c Release -r win-x64 --self-contained true \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
# Output: bin/Release/net8.0-windows/win-x64/publish/DevLauncher.exe
```

---

## How It Works

### Real Process Execution
Every "▶ Launch" click calls `ProcessExecutionService.LaunchPresetAsync()` which:

1. Loops through the preset's `LaunchCommand` list
2. Creates a `Process` with `RedirectStandardOutput = true`
3. Streams stdout/stderr **live** into the terminal panel
4. For **background** commands (e.g. `npm run dev`) — process stays alive, app continues
5. For **foreground** commands (e.g. `npm install`) — waits for exit code 0 before proceeding

```
Click ▶ Launch
    └─> ProcessExecutionService
            └─> cmd.exe /c "npm install"      ← foreground, wait
            └─> cmd.exe /c "npm run dev"      ← background, keep alive
            └─> cmd.exe /c "npm run server"   ← background, keep alive
            └─> Open http://localhost:3000 in default browser
```

### Kill Processes
The sidebar shows all active background processes. Click **✕** next to any
process to kill it (and its child tree). **Stop All** kills everything.

### Persist Custom Presets
Custom presets are saved to:
```
%AppData%\DevLauncher\presets.json
```
They reload automatically on next launch.

### Export Scripts
Each preset can be exported as:
- **`.sh`** — bash script for macOS/Linux
- **`.bat`** — batch file for Windows

---

## Adding a New Preset in Code
Edit `PresetStorageService.GetBuiltInPresets()`:
```csharp
new LaunchPreset
{
    Name = "My Custom Stack",
    Icon = "🔥",
    Category = "Dev",
    Description = "Start my project",
    WorkingDirectory = @"C:\projects\myapp",
    OpenUrlAfterLaunch = "http://localhost:4000",
    Commands =
    {
        new() { Command = "npm install", Description = "Install deps" },
        new() { Command = "npm run dev", Description = "Start dev server",
                RunInBackground = true },
    }
}
```

---

## NuGet Packages

| Package | Purpose |
|---------|---------|
| `CommunityToolkit.Mvvm 8.2.2` | `[ObservableProperty]`, `[RelayCommand]`, source generators |
| `Newtonsoft.Json 13.0.3` | Serialize/deserialize presets to JSON |

---

## Extending the App

| Feature | Where to add |
|---------|-------------|
| New built-in preset | `PresetStorageService.GetBuiltInPresets()` |
| New terminal color | `TerminalTypeToColorConverter.cs` |
| New category | `Categories` list in `MainViewModel` + `CategoryToBadgeColorConverter` |
| Startup on Windows login | Add registry key in `App.xaml.cs` `OnStartup()` |
| Global hotkey to launch | Use `RegisterHotKey` Win32 API in `MainWindow.xaml.cs` |
| System tray icon | Add `NotifyIcon` via `System.Windows.Forms` reference |
