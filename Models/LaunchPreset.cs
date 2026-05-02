using System.Collections.ObjectModel;

namespace DevLauncher.Models;

public class LaunchPreset
{
    public string Id                  { get; set; } = Guid.NewGuid().ToString("N");
    public string Name                { get; set; } = string.Empty;
    public string Description         { get; set; } = string.Empty;
    public string Icon                { get; set; } = "⚡";
    public string Category            { get; set; } = "Dev";
    public string WorkingDirectory    { get; set; } = string.Empty;
    public string OpenUrlAfterLaunch  { get; set; } = string.Empty;
    public bool   IsCustom            { get; set; } = false;

    // Team preset metadata
    public bool   IsTeamPreset        { get; set; } = false;
    public string LastEditedBy        { get; set; } = string.Empty;
    public DateTime LastEditedAt      { get; set; } = DateTime.UtcNow;

    public ObservableCollection<LaunchCommand> Commands { get; set; } = new();

    // null-safe: old JSON without this field deserialises to null → fall back to empty schedule
    private PresetSchedule _schedule = new();
    public  PresetSchedule  Schedule
    {
        get => _schedule;
        set => _schedule = value ?? new();
    }
}
