using System.Collections.ObjectModel;
using System.Diagnostics;

namespace DevLauncher.Models;

/// <summary>
/// Represents one running preset — shown as a tab in the terminal panel.
/// Each session has its own output lines, its own process list, and its own CancellationToken.
/// </summary>
public class TerminalSession
{
    public string   Id          { get; } = Guid.NewGuid().ToString();
    public string   PresetName  { get; set; } = string.Empty;
    public string   PresetIcon  { get; set; } = "⚡";
    public DateTime StartedAt   { get; set; } = DateTime.Now;
    public bool     IsRunning   { get; set; } = true;
    public bool     HasError    { get; set; } = false;

    public ObservableCollection<TerminalLine>  Lines     { get; } = new();
    public List<Process>                       Processes { get; } = new();

    public string TabLabel => $"{PresetIcon} {PresetName}";

    public string StatusLabel => IsRunning ? "● Running" : (HasError ? "✗ Failed" : "✓ Done");
}
