using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevLauncher.Models;
using DevLauncher.Services;

// Explicit aliases — UseWindowsForms=true causes System.Windows.Forms to
// conflict with System.Windows types across the entire project.
using WpfMessageBox       = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage  = System.Windows.MessageBoxImage;
using WpfMessageBoxResult = System.Windows.MessageBoxResult;
using WpfClipboard        = System.Windows.Clipboard;
using WpfApplication      = System.Windows.Application;
using WinForms            = System.Windows.Forms;

namespace DevLauncher.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // ── Services ──────────────────────────────────────────────────
    private readonly ProcessExecutionService  _executor        = new();
    private readonly PresetStorageService     _storage         = new();
    private readonly FirebaseService          _firebase        = new();
    private readonly WorkspaceStorageService  _wsStorage       = new();
    private readonly SchedulerService         _scheduler       = new();

    // ── Collections ───────────────────────────────────────────────
    public ObservableCollection<TerminalSession>   Sessions        { get; } = new();
    public ObservableCollection<LaunchPreset>      AllPresets      { get; } = new();
    public ObservableCollection<LaunchPreset>      FilteredPresets { get; } = new();
    public ObservableCollection<LaunchPreset>      TeamPresets     { get; } = new();
    public ObservableCollection<ProcessMonitorItem> MonitorProcesses { get; } = new();

    // ── General state ─────────────────────────────────────────────
    [ObservableProperty] private TerminalSession? _activeSession;
    [ObservableProperty] private string  _searchText    = string.Empty;
    [ObservableProperty] private string  _activeFilter  = "All";
    [ObservableProperty] private string  _statusText    = "Ready — launch a preset to begin.";
    [ObservableProperty] private string  _currentTime   = DateTime.Now.ToString("HH:mm:ss");
    [ObservableProperty] private bool    _hasNoSessions = true;
    [ObservableProperty] private string  _activeSessionInput = string.Empty;
    [ObservableProperty] private bool    _shellCursorVisible = false;

    // ── Workspace state ───────────────────────────────────────────
    [ObservableProperty] private bool   _isWorkspaceConnected = false;
    [ObservableProperty] private bool   _isWorkspaceJoined    = false;
    [ObservableProperty] private bool   _isConnecting         = false;
    [ObservableProperty] private string _workspaceCode        = string.Empty;
    [ObservableProperty] private string _newWorkspaceName      = string.Empty;
    [ObservableProperty] private string _workspaceName        = string.Empty;
    [ObservableProperty] private string _workspaceStatus      = "Not connected";
    [ObservableProperty] private string _userDisplayName      = string.Empty;

    private WorkspaceSettings _wsSettings = new();

    // ── Builder ───────────────────────────────────────────────────
    [ObservableProperty] private string _newPresetName        = string.Empty;
    [ObservableProperty] private string _newPresetIcon        = "⚡";
    [ObservableProperty] private string _newPresetCategory    = "Dev";
    [ObservableProperty] private string _newPresetDescription = string.Empty;
    [ObservableProperty] private string _newPresetWorkingDir  = string.Empty;
    [ObservableProperty] private string _newPresetOpenUrl     = string.Empty;
    [ObservableProperty] private string _newCommandText       = string.Empty;

    public ObservableCollection<string> NewPresetCommands { get; } = new();
    public List<string> Categories    { get; } = new() { "Dev", "DevOps", "Data", "Design" };
    public List<string> FilterOptions { get; } = new() { "All", "Dev", "DevOps", "Data", "Design" };

    private readonly Dictionary<string, CancellationTokenSource> _sessionCts = new();

    // ─────────────────────────────────────────────────────────────
    public MainViewModel()
    {
        LoadPresets();
        LoadWorkspaceSettings();
        StartClock();
        StartMonitorTimer();
        _scheduler.Start(() => AllPresets, p => LaunchPreset(p));
        // Delay startup presets so the UI is fully loaded first
        Task.Delay(2000).ContinueWith(_ => Dispatch(_scheduler.RunStartupPresets));

        _firebase.PresetsUpdated          += OnFirebasePresetsUpdated;
        _firebase.ConnectionStatusChanged += s => WorkspaceStatus = s;

        // Auto-connect if previously joined
        if (_wsSettings.IsJoined && _wsSettings.AutoSync)
            _ = Task.Run(AutoReconnectAsync);
    }

    // ═══════════════════════════════════════════════════════════════
    // LAUNCH
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private void LaunchPreset(LaunchPreset? preset)
    {
        if (preset == null) return;

        var session = new TerminalSession
        {
            PresetName       = preset.Name,
            PresetIcon       = preset.Icon,
            StartedAt        = DateTime.Now,
            IsRunning        = true,
            WorkingDirectory = preset.WorkingDirectory,
        };

        var cts = new CancellationTokenSource();
        _sessionCts[session.Id] = cts;

        Sessions.Add(session);
        ActiveSession = session;
        HasNoSessions = false;
        StatusText    = $"Launched: {preset.Name}";

        _ = Task.Run(async () =>
        {
            await _executor.RunSessionAsync(
                preset,
                session,
                () => OnSessionUpdated(session),
                cts.Token);

            bool cancelled = cts.IsCancellationRequested;
            _sessionCts.Remove(session.Id);

            // Start persistent shell after preset finishes (unless cancelled/closed)
            if (!cancelled)
            {
                await _executor.StartInteractiveShellAsync(
                    session,
                    CancellationToken.None,
                    onShellReady: () => Dispatch(() =>
                    {
                        if (ActiveSession == session) ShellCursorVisible = true;
                    }),
                    onPromptDetected: () => Dispatch(() =>
                    {
                        // Shell finished a command and is idle again
                        session.IsRunning = false;
                        if (ActiveSession == session) ShellCursorVisible = true;
                    }));
                // Shell exited — hide cursor for this session
                Dispatch(() =>
                {
                    session.IsRunning = false;
                    if (ActiveSession == session) ShellCursorVisible = false;
                });
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // SESSION MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private void CloseSession(TerminalSession? session)
    {
        if (session == null) return;
        if (_sessionCts.TryGetValue(session.Id, out var cts)) { cts.Cancel(); _sessionCts.Remove(session.Id); }
        _executor.KillSession(session);
        Sessions.Remove(session);
        ActiveSession = Sessions.LastOrDefault();
        HasNoSessions = Sessions.Count == 0;
        StatusText    = Sessions.Count == 0 ? "Ready — launch a preset to begin." : $"Active sessions: {Sessions.Count}";
    }

    [RelayCommand]
    private void CloseAllSessions()
    {
        foreach (var s in Sessions.ToList())
        {
            if (_sessionCts.TryGetValue(s.Id, out var cts)) { cts.Cancel(); _sessionCts.Remove(s.Id); }
            _executor.KillSession(s);
        }
        Sessions.Clear();
        ActiveSession      = null;
        HasNoSessions      = true;
        ShellCursorVisible = false;
        StatusText         = "All sessions closed.";
    }

    [RelayCommand]
    private void ClearSession(TerminalSession? session) => session?.Lines.Clear();

    [RelayCommand]
    private void SendSessionCommand()
    {
        var session = ActiveSession;
        if (session?.ShellWriter == null || string.IsNullOrWhiteSpace(ActiveSessionInput)) return;

        var cmd    = ActiveSessionInput.Trim();
        var writer = session.ShellWriter;

        // Finalize the in-progress line: strip cursor, keep as history
        if (session.CurrentInputLine != null)
        {
            session.CurrentInputLine.Text = $"$ {cmd}";
            session.CurrentInputLine = null;
        }

        // Mark running + clear previous error — outline turns green, status → "● Running"
        session.IsRunning = true;
        session.HasError  = false;

        // Clear input AFTER IsRunning=true so OnActiveSessionInputChanged sees IsRunning=true
        // and correctly keeps the cursor hidden (command is in-flight, not idle)
        ActiveSessionInput = string.Empty;

        _ = Task.Run(() => { try { writer.WriteLine(cmd); } catch { } });
    }

    [RelayCommand]
    private void KillMonitorProcess(ProcessMonitorItem? item)
    {
        if (item == null) return;
        try { item.Process.Kill(entireProcessTree: true); } catch { }
    }

    // ─── Terminal Search ───────────────────────────────────────────
    [ObservableProperty] private int _searchMatchIndex = 0;
    [ObservableProperty] private int _searchMatchCount = 0;

    [RelayCommand] private void NextMatch() => SearchMatchIndex = SearchMatchCount > 0 ? (SearchMatchIndex + 1) % SearchMatchCount : 0;
    [RelayCommand] private void PrevMatch() => SearchMatchIndex = SearchMatchCount > 0 ? (SearchMatchIndex - 1 + SearchMatchCount) % SearchMatchCount : 0;
    [RelayCommand] private void CloseSearch() { /* handled in code-behind */ }

    // ═══════════════════════════════════════════════════════════════
    // WORKSPACE — FIREBASE
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private void CreateWorkspace()
    {
        if (string.IsNullOrWhiteSpace(WorkspaceCode))
        {
            ShowMessage("Please enter a workspace code.", "Create Workspace");
            return;
        }
        if (string.IsNullOrWhiteSpace(UserDisplayName))
        {
            ShowMessage("Please enter your display name.", "Create Workspace");
            return;
        }
        if (!FirebaseService.IsConfigured)
        {
            ShowMessage("Firebase is not configured yet.\nOpen Services/FirebaseService.cs and replace the placeholder values.\nSee FIREBASE_SETUP.md for instructions.", "Firebase Not Configured");
            return;
        }

        // Creating = connecting with a new code. Firebase auto-creates it on first write.
        _ = Task.Run(async () =>
        {
            IsConnecting    = true;
            WorkspaceStatus = "Creating workspace...";

            try
            {
                bool connected = await _firebase.ConnectAsync();
                if (!connected) { IsConnecting = false; ShowMessage(WorkspaceStatus, "Failed"); return; }

                // Use the name if provided, fallback to code
                string name = string.IsNullOrWhiteSpace(NewWorkspaceName)
                    ? WorkspaceCode.ToUpperInvariant().Trim()
                    : NewWorkspaceName.Trim();

                await _firebase.CreateWorkspaceAsync(WorkspaceCode.ToUpperInvariant().Trim(), name);

                bool joined = await _firebase.JoinWorkspaceAsync(WorkspaceCode.ToUpperInvariant().Trim());
                if (!joined) { IsConnecting = false; ShowMessage("Failed to join after creating.", "Error"); return; }

                IsWorkspaceConnected = true;
                IsWorkspaceJoined    = true;

                _wsSettings.WorkspaceCode   = WorkspaceCode.ToUpperInvariant().Trim();
                _wsSettings.UserDisplayName = UserDisplayName;
                _wsSettings.IsJoined        = true;
                _wsStorage.Save(_wsSettings);

                StatusText = $"✓ Workspace created: {WorkspaceCode}";
                UpdateTeamPresetVisual();
            }
            catch (Exception ex)
            {
                ShowMessage($"Error creating workspace:\n{ex.Message}", "Error");
            }
            finally
            {
                IsConnecting = false;
            }
        });
    }

    [RelayCommand]
    private void ConnectWorkspace()
    {
        _ = Task.Run(ConnectWorkspaceInternalAsync);
    }

    private async Task ConnectWorkspaceInternalAsync()
    {
        if (string.IsNullOrWhiteSpace(WorkspaceCode))
        {
            ShowMessage("Please enter a workspace code.", "Workspace");
            return;
        }
        if (string.IsNullOrWhiteSpace(UserDisplayName))
        {
            ShowMessage("Please enter your display name so teammates know who made changes.", "Workspace");
            return;
        }
        if (!FirebaseService.IsConfigured)
        {
            ShowMessage("Firebase is not configured yet.\nOpen Services/FirebaseService.cs and replace the placeholder DatabaseUrl and ApiKey values.\nSee FIREBASE_SETUP.md for instructions.", "Firebase Not Configured");
            return;
        }

        IsConnecting  = true;
        WorkspaceStatus = "Connecting...";

        try
        {
            // Connect to Firebase
            bool connected = await _firebase.ConnectAsync();
            if (!connected)
            {
                IsConnecting = false;
                ShowMessage(WorkspaceStatus, "Connection Failed");
                return;
            }

            IsWorkspaceConnected = true;

            // Join workspace
            bool joined = await _firebase.JoinWorkspaceAsync(WorkspaceCode.ToUpperInvariant().Trim());
            if (!joined)
            {
                IsConnecting = false;
                WpfMessageBox.Show($"Workspace '{WorkspaceCode}' not found.\nAsk your team lead for the correct code.", "Not Found", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
                return;
            }

            IsWorkspaceJoined = true;

            // Save settings
            _wsSettings.WorkspaceCode   = WorkspaceCode.ToUpperInvariant().Trim();
            _wsSettings.UserDisplayName = UserDisplayName;
            _wsSettings.IsJoined        = true;
            _wsSettings.AutoSync        = true;
            _wsStorage.Save(_wsSettings);

            // Load initial presets
            UpdateTeamPresetVisual();

            StatusText = $"✓ Joined workspace: {WorkspaceStatus}";
        }
        catch (Exception ex)
        {
            WorkspaceStatus = $"Error: {ex.Message}";
            WpfMessageBox.Show(ex.Message, "Firebase Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private void DisconnectWorkspace()
    {
        _firebase.LeaveWorkspace();
        IsWorkspaceJoined    = false;
        IsWorkspaceConnected = false;
        TeamPresets.Clear();
        _wsSettings.IsJoined = false;
        _wsStorage.Save(_wsSettings);
        WorkspaceStatus = "Disconnected";
        StatusText      = "Left workspace.";
    }

    // Push a local custom preset up to the team workspace
    [RelayCommand]
    private void SharePreset(LaunchPreset? preset)
    {
        _ = Task.Run(() => SharePresetInternalAsync(preset));
    }

    private async Task SharePresetInternalAsync(LaunchPreset? preset)
    {
        if (preset == null || !IsWorkspaceJoined) return;
        try
        {
            preset.IsTeamPreset = true;
            await _firebase.PushPresetAsync(preset, UserDisplayName);
            UpdateTeamPresetVisual();
            StatusText = $"Shared: {preset.Name} → {WorkspaceCode}";
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"Share failed: {ex.Message}", "Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
        }
    }

    // Pull a team preset down to local custom presets
    [RelayCommand]
    private void PullTeamPreset(LaunchPreset? preset)
    {
        if (preset == null) return;
        var local = new LaunchPreset
        {
            Id                 = Guid.NewGuid().ToString("N"),
            Name               = preset.Name + " (local copy)",
            Description        = preset.Description,
            Icon               = preset.Icon,
            Category           = preset.Category,
            WorkingDirectory   = preset.WorkingDirectory,
            OpenUrlAfterLaunch = preset.OpenUrlAfterLaunch,
            IsCustom           = true,
            IsTeamPreset       = false,
        };
        foreach (var c in preset.Commands)
            local.Commands.Add(new LaunchCommand { Command = c.Command, Description = c.Description, RunInBackground = c.RunInBackground });

        AllPresets.Add(local);
        MakePresetsOrderByLastEdited();
        _storage.SaveCustomPresets(AllPresets);
        ApplyFilter();
        StatusText = $"Pulled to local: {local.Name}";
    }

    // Delete a team preset from Firebase
    [RelayCommand]
    private void DeleteTeamPreset(LaunchPreset? preset)
    {
        _ = Task.Run(() => DeleteTeamPresetInternalAsync(preset));
    }

    private async Task DeleteTeamPresetInternalAsync(LaunchPreset? preset)
    {
        if (preset == null || !IsWorkspaceJoined) return;
        if (WpfMessageBox.Show($"Delete '{preset.Name}' from the team workspace?",
            "Confirm", WpfMessageBoxButton.YesNo, WpfMessageBoxImage.Warning) != WpfMessageBoxResult.Yes) return;
        try
        {
            await _firebase.DeletePresetAsync(preset.Id);
            UpdateTeamPresetVisual();
            StatusText = $"Deleted from workspace: {preset.Name}";
        }
        catch (Exception ex)
        {
            WpfMessageBox.Show($"Delete failed: {ex.Message}", "Error", WpfMessageBoxButton.OK, WpfMessageBoxImage.Error);
        }
    }

    private async void UpdateTeamPresetVisual()
    {
        UpdateTeamPresets(await _firebase.FetchAllPresetsAsync());
    }

    // ═══════════════════════════════════════════════════════════════
    // PRESET MANAGEMENT (LOCAL)
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private void BrowseWorkingDir(LaunchPreset? preset)
    {
        if (preset == null) return;
        var path = BrowseFolder(preset.WorkingDirectory);
        if (path != null)
        {
            preset.WorkingDirectory = path;
            _storage.SaveCustomPresets(AllPresets);
            StatusText = $"Directory set: {path}";
        }
    }

    [RelayCommand]
    private void CopyScript(LaunchPreset? preset)
    {
        if (preset == null) return;
        WpfClipboard.SetText(GenerateBatScript(preset));
        StatusText = $"Copied script: {preset.Name}";
    }

    [RelayCommand]
    private void ExportScript(LaunchPreset? preset)
    {
        if (preset == null) return;
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            FileName   = preset.Name.Replace(" ", "-").ToLower(),
            DefaultExt = ".bat",
            Filter     = "Batch File|*.bat|Shell Script|*.sh|All Files|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            var script = dlg.FilterIndex == 2 ? GenerateShScript(preset) : GenerateBatScript(preset);
            System.IO.File.WriteAllText(dlg.FileName, script);
            StatusText = $"Exported: {dlg.FileName}";
        }
    }

    [RelayCommand]
    private void AddCommand()
    {
        if (string.IsNullOrWhiteSpace(NewCommandText)) return;
        NewPresetCommands.Add(NewCommandText.Trim());
        NewCommandText = string.Empty;
    }

    [RelayCommand]
    private void RemoveCommand(string? cmd) { if (cmd != null) NewPresetCommands.Remove(cmd); }

    [RelayCommand]
    private void SaveCustomPreset()
    {
        if (string.IsNullOrWhiteSpace(NewPresetName))
        {
            WpfMessageBox.Show("Please enter a preset name.", "Validation", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }
        if (NewPresetCommands.Count == 0)
        {
            WpfMessageBox.Show("Please add at least one command.", "Validation", WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
            return;
        }

        var preset = new LaunchPreset
        {
            Name = NewPresetName, Icon = NewPresetIcon,
            Category = NewPresetCategory, Description = NewPresetDescription,
            WorkingDirectory = NewPresetWorkingDir, OpenUrlAfterLaunch = NewPresetOpenUrl,
            IsCustom = true
        };
        foreach (var cmd in NewPresetCommands)
            preset.Commands.Add(new LaunchCommand { Command = cmd });

        AllPresets.Add(preset);
        MakePresetsOrderByLastEdited();
        _storage.SaveCustomPresets(AllPresets);
        ApplyFilter();
        ResetBuilder();
        StatusText = $"Saved: {preset.Name}";
    }

    [RelayCommand]
    private void DeletePreset(LaunchPreset? preset)
    {
        if (preset == null || !preset.IsCustom) return;
        if (WpfMessageBox.Show($"Delete '{preset.Name}'?", "Confirm", WpfMessageBoxButton.YesNo) != WpfMessageBoxResult.Yes) return;
        AllPresets.Remove(preset);
        _storage.SaveCustomPresets(AllPresets);
        ApplyFilter();
    }

    [RelayCommand]
    private void OpenScheduleDialog(LaunchPreset? preset)
    {
        if (preset == null) return;
        var dialog = new DevLauncher.Views.ScheduleDialog(preset)
        {
            Owner = WpfApplication.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
        {
            _storage.SaveCustomPresets(AllPresets);
            ApplyFilter(); // re-render cards so the schedule badge updates
            StatusText = preset.Schedule.Type == Models.ScheduleType.None
                ? $"Schedule cleared: {preset.Name}"
                : $"Scheduled: {preset.Name} — {preset.Schedule.Summary}";
        }
    }

    [RelayCommand]
    private void SetFilter(string filter) => ActiveFilter = filter;

    // ═══════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════════════

    partial void OnSearchTextChanged(string value)   => ApplyFilter();
    partial void OnActiveFilterChanged(string value) => ApplyFilter();

    partial void OnActiveSessionInputChanged(string value)
    {
        var session = ActiveSession;
        if (session?.ShellWriter == null) return;

        if (string.IsNullOrEmpty(value))
        {
            // Cursor only when the shell is idle — not while a command is executing
            ShellCursorVisible = !session.IsRunning;
            if (session.CurrentInputLine != null)
            {
                session.Lines.Remove(session.CurrentInputLine);
                session.CurrentInputLine = null;
            }
            return;
        }

        // User is typing — hide the standalone cursor (▌ is embedded in CurrentInputLine)
        ShellCursorVisible = false;

        if (session.CurrentInputLine == null)
        {
            session.CurrentInputLine = new TerminalLine { Type = TerminalLineType.Command };
            session.Lines.Add(session.CurrentInputLine);
            while (session.Lines.Count > 2000) session.Lines.RemoveAt(0);
        }

        // Update in-place — Text is [ObservableProperty] so the bound TextBlock refreshes
        session.CurrentInputLine.Text = $"$ {value}▌";
    }

    partial void OnActiveSessionChanged(TerminalSession? value)
    {
        // Restore cursor state when switching between sessions
        ShellCursorVisible = value?.ShellWriter != null && string.IsNullOrEmpty(ActiveSessionInput);
    }

    private void OnFirebasePresetsUpdated(List<Models.TeamPreset> presets)
        => UpdateTeamPresets(presets);

    private void UpdateTeamPresets(List<Models.TeamPreset> presets)
    {
        Dispatch(() =>
        {
            TeamPresets.Clear();
            foreach (var tp in presets)
                TeamPresets.Add(tp.ToLaunchPreset());
        });
        StatusText = $"Team presets synced — {presets.Count} presets";
    }

    private void OnSessionUpdated(TerminalSession session)
    {
        int running = Sessions.Count(s => s.IsRunning);
        StatusText = running > 0
            ? $"{running} session(s) running | {Sessions.Count} total"
            : $"All done — {Sessions.Count} session(s)";
    }

    private void LoadPresets()
    {
       // foreach (var p in _storage.GetBuiltInPresets()) AllPresets.Add(p);
        foreach (var p in _storage.LoadCustomPresets())  AllPresets.Add(p);
        MakePresetsOrderByLastEdited();
        ApplyFilter();
    }
    
    private void MakePresetsOrderByLastEdited()
    {
      var sorted = AllPresets
        .OrderByDescending(p => p.LastEditedAt)
        .ToList();

    AllPresets.Clear();

    foreach (var item in sorted)
    {
        AllPresets.Add(item);
    }
    }
    private void LoadWorkspaceSettings()
    {
        _wsSettings      = _wsStorage.Load();
        WorkspaceCode    = _wsSettings.WorkspaceCode;
        UserDisplayName  = _wsSettings.UserDisplayName;
        WorkspaceName    = _wsSettings.WorkspaceName;
    }

    private async Task AutoReconnectAsync()
    {
        try
        {
            await Task.Delay(1500); // Let UI load first

            // Only auto-reconnect if user previously joined AND firebase is configured
            if (!_wsSettings.IsJoined
                || string.IsNullOrWhiteSpace(_wsSettings.WorkspaceCode)
                || !FirebaseService.IsConfigured)
                return;

            WorkspaceCode   = _wsSettings.WorkspaceCode;
            UserDisplayName = _wsSettings.UserDisplayName;
            await ConnectWorkspaceInternalAsync();
        }
        catch { /* Never crash on auto-reconnect */ }
    }

    /// <summary>Runs action on UI thread — safe to call from any thread.</summary>
    private static void Dispatch(Action action)
        => System.Windows.Application.Current?.Dispatcher.Invoke(action);

    /// <summary>
    /// Thread-safe MessageBox — safe to call from background threads.
    /// </summary>
    private static void ShowMessage(string msg, string title = "DevLauncher")
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            WpfMessageBox.Show(msg, title, WpfMessageBoxButton.OK, WpfMessageBoxImage.Information));
    }

    private void ApplyFilter()
    {
        Dispatch(() =>
        {
            FilteredPresets.Clear();
            foreach (var p in AllPresets)
            {
                bool cat    = ActiveFilter == "All" || string.Equals(p.Category, ActiveFilter, StringComparison.OrdinalIgnoreCase);
                bool search = string.IsNullOrWhiteSpace(SearchText)
                           || p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                           || p.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
                if (cat && search) FilteredPresets.Add(p);
            }
        });
    }

    private void ResetBuilder()
    {
        NewPresetName = string.Empty; NewPresetIcon = "⚡"; NewPresetCategory = "Dev";
        NewPresetDescription = string.Empty; NewPresetWorkingDir = string.Empty;
        NewPresetOpenUrl = string.Empty; NewPresetCommands.Clear();
    }

    public static string? BrowseFolder(string? initialDir = null)
    {
        using var dlg = new WinForms.FolderBrowserDialog
        {
            Description            = "Select working directory for this preset",
            UseDescriptionForTitle = true,
            ShowNewFolderButton    = true,
        };
        if (!string.IsNullOrWhiteSpace(initialDir) && System.IO.Directory.Exists(initialDir))
            dlg.InitialDirectory = initialDir;
        return dlg.ShowDialog() == WinForms.DialogResult.OK ? dlg.SelectedPath : null;
    }

    private static string GenerateBatScript(LaunchPreset p)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine($"title {p.Name}");
        if (!string.IsNullOrWhiteSpace(p.WorkingDirectory)) sb.AppendLine($"cd /d \"{p.WorkingDirectory}\"");
        foreach (var c in p.Commands) { sb.AppendLine($"echo $ {c.Command}"); sb.AppendLine(c.Command); }
        return sb.ToString();
    }

    private static string GenerateShScript(LaunchPreset p)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("#!/bin/bash");
        sb.AppendLine($"# {p.Name}");
        if (!string.IsNullOrWhiteSpace(p.WorkingDirectory)) sb.AppendLine($"cd \"{p.WorkingDirectory}\"");
        foreach (var c in p.Commands) { sb.AppendLine($"echo '$ {c.Command}'"); sb.AppendLine(c.Command); }
        return sb.ToString();
    }

    private void StartClock()
    {
        var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        t.Tick += (_, _) => CurrentTime = DateTime.Now.ToString("HH:mm:ss");
        t.Start();
    }

    private void StartMonitorTimer()
    {
        var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        t.Tick += (_, _) => RefreshMonitor();
        t.Start();
    }

    private void RefreshMonitor()
    {
        // Remove entries that are dead OR belong to a session that is no longer running
        for (int i = MonitorProcesses.Count - 1; i >= 0; i--)
        {
            MonitorProcesses[i].Refresh();
            var owner = Sessions.FirstOrDefault(s => s.Id == MonitorProcesses[i].SessionId);
            if (!MonitorProcesses[i].IsAlive || owner?.IsRunning != true)
                MonitorProcesses.RemoveAt(i);
        }

        // Add alive processes from currently-running sessions only.
        // Skip exited ones — session.Processes accumulates all historical processes
        // and dead ones would cause phantom duplicate rows.
        var tracked = MonitorProcesses.Select(m => m.Process).ToHashSet();
        foreach (var session in Sessions.Where(s => s.IsRunning))
        {
            foreach (var proc in session.Processes.ToList())
            {
                try
                {
                    if (proc.HasExited || tracked.Contains(proc)) continue;
                    MonitorProcesses.Add(new ProcessMonitorItem(session, proc));
                }
                catch { /* process may have just exited */ }
            }
        }
    }
}