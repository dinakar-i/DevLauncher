# DevLauncher — Feature Roadmap

> A living document of planned features for DevLauncher v2.0+
> Built with .NET 8 WPF · One-click dev workspace automation

---

## Table of Contents

- [Currently Shipped](#-currently-shipped-v10)
- [High Value — Core Productivity](#-high-value--core-productivity)
- [Visibility & Monitoring](#-visibility--monitoring)
- [Preset Management](#-preset-management)
- [UX Polish](#-ux-polish)
- [How to Contribute](#-how-to-contribute)

---

## ✅ Currently Shipped (v1.0)

| Feature | Description |
|---------|-------------|
| Preset Library | 8 built-in presets for Node.js, Python, Docker, Kubernetes, React, Jupyter, CI/CD, DB stack |
| Multi-Session Terminals | Each preset launch opens its own terminal tab — run unlimited presets simultaneously |
| Live Output Streaming | Real stdout/stderr streamed line-by-line into the in-app terminal panel |
| Background Processes | Long-running commands (dev servers, watchers) stay alive independently |
| Kill Controls | Kill individual sessions or stop all with one click |
| Custom Preset Builder | Build your own presets with commands, working directory, and auto-open URL |
| Icon Picker | 200+ categorized emoji icons to visually identify presets |
| Script Export | Export any preset as a `.bat` or `.sh` shell script |
| Resizable Window | Drag to resize, drag title bar to move, custom min/max/close controls |
| Persistent Storage | Custom presets saved to `%AppData%\DevLauncher\presets.json` |
| Dark Theme | Full custom dark UI with accent colors, matching all controls |

---

## 🔥 High Value — Core Productivity

### 1. Project Directory Browser
**Priority:** High

Instead of typing the working directory path manually, add a folder-picker button next to the Working Directory field. Clicking it opens the native Windows `FolderBrowserDialog` so you can navigate and select the project folder visually.

**Implementation hint:**
```csharp
var dlg = new System.Windows.Forms.FolderBrowserDialog();
if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
    preset.WorkingDirectory = dlg.SelectedPath;
```

---

### 2. Startup on Windows Login
**Priority:** High

A toggle switch in a Settings panel that registers DevLauncher to auto-start when Windows boots. Optionally, mark specific presets as "auto-launch on startup" so your entire dev environment is running by the time you sit down.

**Implementation hint:**
```csharp
// Add to registry
Registry.CurrentUser
    .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)
    .SetValue("DevLauncher", Application.ExecutablePath);

// Remove from registry
Registry.CurrentUser
    .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)
    .DeleteValue("DevLauncher", false);
```

---

### 3. Global Quick Launch Hotkey
**Priority:** High

A system-wide keyboard shortcut (default: `Ctrl + Shift + L`) that works even when DevLauncher is minimized or in the system tray. Pressing it brings up a compact spotlight-style overlay where you can type a preset name and hit Enter to launch it instantly.

**Implementation hint:**
```csharp
// Register via Win32 RegisterHotKey API
[DllImport("user32.dll")]
static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

// MOD_CONTROL | MOD_SHIFT = 0x0003, L = 0x4C
RegisterHotKey(handle, 1, 0x0003, 0x4C);
```

---

### 4. Preset Scheduling
**Priority:** Medium

Run any preset automatically on a schedule — useful for nightly builds, database backups, or spinning up a morning standup environment. Each preset can have an optional cron-style schedule or a specific daily time trigger.

**Planned UI:** A clock icon on each preset card opens a schedule dialog with options:
- Run once at a specific time
- Run daily at HH:MM
- Run on selected weekdays
- Run on Windows startup

---

### 5. Environment Variables per Preset
**Priority:** High

Each preset can define its own `KEY=VALUE` environment variables that get injected into the process environment before any commands run. No more manually editing `.env` files or switching profiles.

**Planned UI:** An "Env Vars" section in the custom preset builder with a key/value table editor.

**Implementation hint:**
```csharp
// In ProcessStartInfo setup
foreach (var (key, value) in preset.EnvironmentVariables)
    psi.Environment[key] = value;
```

---

## 📊 Visibility & Monitoring

### 6. Process Health Dashboard
**Priority:** Medium

A dedicated "Monitor" tab showing a live table of all running session processes with:
- Process name and PID
- CPU usage %
- Memory usage (MB)
- Uptime since launch
- One-click kill button per process

**Implementation hint:**
```csharp
var proc = Process.GetProcessById(pid);
var cpu  = proc.TotalProcessorTime;
var mem  = proc.WorkingSet64 / 1024 / 1024; // MB
```

---

### 7. Desktop Notification on Completion
**Priority:** Medium

When a foreground command finishes (e.g. `npm install` completes, tests pass or fail), show a Windows toast notification so you can switch away and come back only when needed.

**Implementation hint:**
```xml
<!-- Add NuGet package -->
<PackageReference Include="Microsoft.Toolkit.Uwp.Notifications" Version="7.1.3"/>
```
```csharp
new ToastContentBuilder()
    .AddText($"✓ {preset.Name} finished")
    .AddText($"Exit code: {exitCode}")
    .Show();
```

---

### 8. Terminal Search
**Priority:** Medium

`Ctrl + F` inside any active terminal session opens an inline search bar at the top of the terminal panel. Matching lines are highlighted in amber. Navigate between matches with `F3` / `Shift+F3`.

**Planned UI:** A thin search bar that slides down from the terminal header when `Ctrl+F` is pressed, with match count indicator (e.g. "3 / 12 matches").

---

### 9. Session Log Export
**Priority:** Low

A "Save Log" button on each terminal session tab that writes the full output to a timestamped `.log` file. Useful for sharing build output, debugging CI issues, or keeping audit trails.

**Default save path:**
```
%AppData%\DevLauncher\Logs\{PresetName}_{yyyy-MM-dd_HH-mm-ss}.log
```

---

## 🗂️ Preset Management

### 10. Preset Groups / Folders
**Priority:** Medium

Organize presets into collapsible named folders in the sidebar — e.g. "Client A", "Internal Tools", "Infrastructure". Groups can be expanded or collapsed and dragged to reorder.

**Planned data model:**
```json
{
  "groups": [
    {
      "name": "Client A",
      "collapsed": false,
      "presetIds": ["id-1", "id-2"]
    }
  ]
}
```

---

### 11. Import / Export Presets
**Priority:** High

Share your entire preset library with teammates as a single `.json` file. Import merges incoming presets with your existing ones (with duplicate detection). Makes onboarding new team members instant — share one file and everyone has the same launcher setup.

**Export format:** The existing `presets.json` structure, exported via a Save dialog.
**Import:** File picker → parse → merge → save.

---

### 12. Preset Templates (Quick Start)
**Priority:** Medium

When creating a new custom preset, offer one-click starter templates that pre-fill all the fields for common stacks. Saves setup time for new projects.

**Planned templates:**

| Template | Pre-filled commands |
|----------|-------------------|
| React App | `npm install` → `npm start` |
| Next.js | `npm install` → `npm run dev` |
| Django | `pip install -r requirements.txt` → `python manage.py migrate` → `python manage.py runserver` |
| .NET API | `dotnet restore` → `dotnet watch run` |
| Docker Compose | `docker-compose pull` → `docker-compose up -d` |
| Go Service | `go mod tidy` → `go run main.go` |

---

### 13. Duplicate Preset
**Priority:** Low

Right-click any preset card → "Duplicate" → creates a copy with "Copy of" prefix, ready to edit. Saves time when setting up similar environments (e.g. dev vs staging with different env vars).

---

## 🎨 UX Polish

### 14. System Tray Icon
**Priority:** High

Minimize DevLauncher to the system tray instead of the taskbar. Right-click the tray icon for a context menu showing all presets — click any to launch directly without opening the main window.

**Implementation hint:**
```csharp
// Add System.Windows.Forms reference
var tray = new NotifyIcon
{
    Icon    = new Icon("Assets/icon.ico"),
    Visible = true,
    Text    = "DevLauncher"
};
tray.ContextMenuStrip = BuildTrayMenu(); // one item per preset
```

---

### 15. Per-Session Terminal Colors
**Priority:** Low

Each terminal session tab gets a distinct accent color (cycling through a palette of 8 colors) so you can instantly tell sessions apart when 4-5 are running simultaneously. The colored dot on the tab header and the sidebar badge both use the session color.

**Planned palette:**
`#00E5FF` · `#10B981` · `#F59E0B` · `#EF4444` · `#7C3AED` · `#EC4899` · `#3B82F6` · `#84CC16`

---

### 16. Command History Dropdown
**Priority:** Medium

The command input in the custom preset builder remembers the last 50 commands typed across all presets (stored in `%AppData%\DevLauncher\history.json`). Clicking the input or pressing `↑` shows a dropdown of recent commands — just like shell history.

---

### 17. Preset Card Drag to Reorder
**Priority:** Low

Drag preset cards to reorder them within the grid. Order is persisted to `presets.json`. Useful for keeping your most-used presets at the top.

---

### 18. Inline Command Edit on Cards
**Priority:** Low

Double-click a command chip on any preset card to edit it inline without going into the full builder — quick fixes without opening the Custom tab.

---

## 🛠️ How to Contribute

1. Fork the repo and create a feature branch: `git checkout -b feature/system-tray`
2. Pick a feature from this document
3. Implement it following the existing MVVM pattern:
   - Data model changes → `Models/`
   - Business logic → `Services/`
   - UI state → `ViewModels/MainViewModel.cs`
   - UI → `Views/MainWindow.xaml`
4. Test with at least 2 different preset types
5. Submit a pull request referencing the feature number from this doc

---

## 📦 Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | .NET 8 WPF |
| MVVM | CommunityToolkit.Mvvm 8.2.2 |
| Persistence | Newtonsoft.Json 13.0.3 |
| Process execution | `System.Diagnostics.Process` |
| OS | Windows 10 / 11 |

---

*DevLauncher v1.0 — Built for IT professionals who are tired of typing the same terminal commands every morning.*

🧩 3. Automation Types (Modules)
Each as a pluggable module:

✅ Terminal / CMD / PowerShell automation
✅ GUI Desktop App automation (FlaUI)
✅ Browser automation (Playwright)
✅ File & Folder automation (watch, move, rename, zip)
✅ Email automation (send, read, filter)
✅ Office Docs (Excel, Word, PowerPoint)
✅ System Monitor (CPU, RAM, disk, processes)
✅ Scheduled Tasks (time-based triggers)
✅ API / Webhook automation (call REST APIs)
✅ AI-Powered automation (Claude integration)