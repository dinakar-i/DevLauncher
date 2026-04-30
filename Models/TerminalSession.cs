using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevLauncher.Models;

public partial class TerminalSession : ObservableObject
{
    public string   Id               { get; } = Guid.NewGuid().ToString();
    public string   PresetName       { get; set; } = string.Empty;
    public string   PresetIcon       { get; set; } = "⚡";
    public DateTime StartedAt        { get; set; } = DateTime.Now;
    public string   WorkingDirectory { get; set; } = string.Empty;

    // ObservableProperty so DataTriggers in the UI fire when these change
    [ObservableProperty] private bool _isRunning = true;
    [ObservableProperty] private bool _hasError  = false;

    // Stdin writer for the persistent interactive shell (null until shell starts)
    public StreamWriter? ShellWriter { get; set; }

    // The line currently being typed — lives inside Lines, updated in-place as user types
    public TerminalLine? CurrentInputLine { get; set; }

    public ObservableCollection<TerminalLine> Lines     { get; } = new();
    public List<Process>                      Processes { get; } = new();

    public string TabLabel    => $"{PresetIcon} {PresetName}";
    public string StatusLabel => IsRunning ? "● Running" : (HasError ? "✗ Exited" : "✓ Done");

    // Notify computed properties when backing fields change
    partial void OnIsRunningChanged(bool value) => OnPropertyChanged(nameof(StatusLabel));
    partial void OnHasErrorChanged(bool value)  => OnPropertyChanged(nameof(StatusLabel));
}
