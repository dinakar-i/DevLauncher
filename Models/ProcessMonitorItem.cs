using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevLauncher.Models;

public partial class ProcessMonitorItem : ObservableObject
{
    private TimeSpan _lastCpuTime;
    private DateTime _lastSampleAt;

    public string  SessionId   { get; }
    public string  SessionName { get; }
    public string  SessionIcon { get; }
    public Process Process     { get; }

    [ObservableProperty] private string _processName = string.Empty;
    [ObservableProperty] private int    _pid;
    [ObservableProperty] private double _cpuPercent;
    [ObservableProperty] private long   _memoryMb;
    [ObservableProperty] private string _uptime = "--:--";
    [ObservableProperty] private bool   _isAlive;

    public ProcessMonitorItem(TerminalSession session, Process process)
    {
        SessionId   = session.Id;
        SessionName = session.PresetName;
        SessionIcon = session.PresetIcon;
        Process     = process;
        Refresh();
    }

    public void Refresh()
    {
        try
        {
            Process.Refresh();
            IsAlive = !Process.HasExited;

            if (!IsAlive)
            {
                CpuPercent = 0;
                return;
            }

            Pid         = Process.Id;
            ProcessName = Process.ProcessName;
            MemoryMb    = Process.WorkingSet64 / 1024 / 1024;

            var up = DateTime.Now - Process.StartTime;
            Uptime = up.TotalHours >= 1
                ? $"{(int)up.TotalHours:D2}:{up.Minutes:D2}:{up.Seconds:D2}"
                : $"{up.Minutes:D2}:{up.Seconds:D2}";

            var now    = DateTime.Now;
            var cpuNow = Process.TotalProcessorTime;

            if (_lastSampleAt != default)
            {
                var elapsed = (now - _lastSampleAt).TotalSeconds;
                if (elapsed > 0)
                    CpuPercent = Math.Round(
                        (cpuNow - _lastCpuTime).TotalSeconds / elapsed / Environment.ProcessorCount * 100, 1);
            }

            _lastCpuTime  = cpuNow;
            _lastSampleAt = now;
        }
        catch
        {
            IsAlive = false;
        }
    }
}
