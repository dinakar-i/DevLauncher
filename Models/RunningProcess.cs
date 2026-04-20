using System.Diagnostics;

namespace DevLauncher.Models;

public class RunningProcess
{
    public string   PresetName  { get; set; } = string.Empty;
    public string   Command     { get; set; } = string.Empty;
    public Process? Process     { get; set; }
    public DateTime StartedAt   { get; set; } = DateTime.Now;
    public bool     IsAlive     => Process != null && !Process.HasExited;
}
