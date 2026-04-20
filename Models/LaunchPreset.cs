using System.Collections.ObjectModel;

namespace DevLauncher.Models;

public class LaunchPreset
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "⚡";
    public string Category { get; set; } = "Dev";
    public string WorkingDirectory { get; set; } = string.Empty;
    public string OpenUrlAfterLaunch { get; set; } = string.Empty;
    public bool IsCustom { get; set; } = false;
    public ObservableCollection<LaunchCommand> Commands { get; set; } = new();
}
