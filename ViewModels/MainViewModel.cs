using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevLauncher.Models;
using DevLauncher.Services;

namespace DevLauncher.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ProcessExecutionService _executor = new();
    private readonly PresetStorageService    _storage  = new();

    // Each active launch gets its own session tab
    public ObservableCollection<TerminalSession> Sessions        { get; } = new();
    public ObservableCollection<LaunchPreset>    AllPresets      { get; } = new();
    public ObservableCollection<LaunchPreset>    FilteredPresets { get; } = new();

    [ObservableProperty] private TerminalSession? _activeSession;
    [ObservableProperty] private LaunchPreset?    _selectedPreset;
    [ObservableProperty] private string           _searchText    = string.Empty;
    [ObservableProperty] private string           _activeFilter  = "All";
    [ObservableProperty] private string           _statusText    = "Ready — launch a preset to begin.";
    [ObservableProperty] private string           _currentTime   = DateTime.Now.ToString("HH:mm:ss");
    [ObservableProperty] private bool             _hasNoSessions = true;

    // Builder
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

    // Per-session CTS dictionary so we can cancel individual sessions
    private readonly Dictionary<string, CancellationTokenSource> _sessionCts = new();

    public MainViewModel()
    {
        LoadPresets();
        StartClock();
    }

    // ─── Launch ───────────────────────────────────────────────────

    [RelayCommand]
    private void LaunchPreset(LaunchPreset? preset)
    {
        if (preset == null) return;

        // Always create a brand-new session tab — never blocks previous ones
        var session = new TerminalSession
        {
            PresetName = preset.Name,
            PresetIcon = preset.Icon,
            StartedAt  = DateTime.Now,
            IsRunning  = true,
        };

        var cts = new CancellationTokenSource();
        _sessionCts[session.Id] = cts;

        Sessions.Add(session);
        ActiveSession = session;
        HasNoSessions = false;
        StatusText    = $"Launched: {preset.Name}";

        // Fire-and-forget — runs on background thread, button stays enabled immediately
        _ = Task.Run(async () =>
        {
            await _executor.RunSessionAsync(
                preset,
                session,
                () => OnSessionUpdated(session),
                cts.Token);

            _sessionCts.Remove(session.Id);
        });
    }

    // ─── Session management ───────────────────────────────────────

    [RelayCommand]
    private void SelectSession(TerminalSession? session)
    {
        if (session != null)
            ActiveSession = session;
    }

    [RelayCommand]
    private void CloseSession(TerminalSession? session)
    {
        if (session == null) return;

        // Cancel and kill
        if (_sessionCts.TryGetValue(session.Id, out var cts))
        {
            cts.Cancel();
            _sessionCts.Remove(session.Id);
        }
        _executor.KillSession(session);

        Sessions.Remove(session);

        // Switch active session
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
        ActiveSession = null;
        HasNoSessions = true;
        StatusText    = "All sessions closed.";
    }

    [RelayCommand]
    private void ClearSession(TerminalSession? session)
    {
        session?.Lines.Clear();
    }

    // ─── Preset management ────────────────────────────────────────

    [RelayCommand]
    private void CopyScript(LaunchPreset? preset)
    {
        if (preset == null) return;
        Clipboard.SetText(GenerateBatScript(preset));
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
    private void RemoveCommand(string? cmd)
    {
        if (cmd != null) NewPresetCommands.Remove(cmd);
    }

    [RelayCommand]
    private void SaveCustomPreset()
    {
        if (string.IsNullOrWhiteSpace(NewPresetName))
        {
            MessageBox.Show("Please enter a preset name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (NewPresetCommands.Count == 0)
        {
            MessageBox.Show("Please add at least one command.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        _storage.SaveCustomPresets(AllPresets);
        ApplyFilter();
        ResetBuilder();
        StatusText = $"Saved: {preset.Name}";
    }

    [RelayCommand]
    private void DeletePreset(LaunchPreset? preset)
    {
        if (preset == null || !preset.IsCustom) return;
        if (MessageBox.Show($"Delete '{preset.Name}'?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        AllPresets.Remove(preset);
        _storage.SaveCustomPresets(AllPresets);
        ApplyFilter();
    }

    [RelayCommand]
    private void SetFilter(string filter) => ActiveFilter = filter;

    // ─── Partial callbacks ────────────────────────────────────────
    partial void OnSearchTextChanged(string value)   => ApplyFilter();
    partial void OnActiveFilterChanged(string value) => ApplyFilter();

    // ─── Helpers ──────────────────────────────────────────────────

    private void OnSessionUpdated(TerminalSession session)
    {
        // If this is the active session, nothing extra needed (binding updates automatically)
        // Update status bar
        int running = Sessions.Count(s => s.IsRunning);
        StatusText = running > 0
            ? $"{running} session(s) running | {Sessions.Count} total"
            : $"All done — {Sessions.Count} session(s)";
    }

    private void LoadPresets()
    {
       // foreach (var p in _storage.GetBuiltInPresets()) AllPresets.Add(p);
        foreach (var p in _storage.LoadCustomPresets())  AllPresets.Add(p);
        ApplyFilter();
    }

    private void ApplyFilter()
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
    }

    private void ResetBuilder()
    {
        NewPresetName = string.Empty; NewPresetIcon = "⚡"; NewPresetCategory = "Dev";
        NewPresetDescription = string.Empty; NewPresetWorkingDir = string.Empty;
        NewPresetOpenUrl = string.Empty; NewPresetCommands.Clear();
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
}
