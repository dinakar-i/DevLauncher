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

    public List<PresetSchedule> Schedules { get; set; } = new();
}
