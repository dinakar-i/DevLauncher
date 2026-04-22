using System.Collections.ObjectModel;

namespace DevLauncher.Models;

/// <summary>
/// The shape of a preset as stored in Firebase Realtime Database.
/// Mirrors LaunchPreset but serialization-friendly (no ObservableCollection).
/// </summary>
public class TeamPreset
{
    public string   Id              { get; set; } = Guid.NewGuid().ToString("N");
    public string   Name            { get; set; } = string.Empty;
    public string   Description     { get; set; } = string.Empty;
    public string   Icon            { get; set; } = "⚡";
    public string   Category        { get; set; } = "Dev";
    public string   WorkingDirectory { get; set; } = string.Empty;
    public string   OpenUrlAfterLaunch { get; set; } = string.Empty;
    public string   LastEditedBy    { get; set; } = string.Empty;
    public DateTime   LastEditedAt    { get; set; } = DateTime.UtcNow;
    public List<TeamCommand> Commands { get; set; } = new();

    /// <summary>Convert to LaunchPreset for use in the app.</summary>
    public LaunchPreset ToLaunchPreset()
    {
        var p = new LaunchPreset
        {
            Id                 = Id,
            Name               = Name,
            Description        = Description,
            Icon               = Icon,
            Category           = Category,
            WorkingDirectory   = WorkingDirectory,
            OpenUrlAfterLaunch = OpenUrlAfterLaunch,
            IsCustom           = true,
            IsTeamPreset       = true,
            LastEditedBy       = LastEditedBy,
            LastEditedAt       = LastEditedAt,
        };
        foreach (var c in Commands)
            p.Commands.Add(new LaunchCommand
            {
                Command         = c.Command,
                Description     = c.Description,
                RunInBackground = c.RunInBackground,
                DelayAfterMs    = c.DelayAfterMs,
            });
        return p;
    }

    /// <summary>Build from a LaunchPreset.</summary>
    public static TeamPreset FromLaunchPreset(LaunchPreset p, string editedBy) => new()
    {
        Id                 = p.Id,
        Name               = p.Name,
        Description        = p.Description,
        Icon               = p.Icon,
        Category           = p.Category,
        WorkingDirectory   = p.WorkingDirectory,
        OpenUrlAfterLaunch = p.OpenUrlAfterLaunch,
        LastEditedBy       = editedBy,
        LastEditedAt       = DateTime.UtcNow,
        Commands           = p.Commands.Select(c => new TeamCommand
        {
            Command         = c.Command,
            Description     = c.Description,
            RunInBackground = c.RunInBackground,
            DelayAfterMs    = c.DelayAfterMs,
        }).ToList(),
    };
}

public class TeamCommand
{
    public string Command         { get; set; } = string.Empty;
    public string Description     { get; set; } = string.Empty;
    public bool   RunInBackground { get; set; } = false;
    public int    DelayAfterMs    { get; set; } = 0;
}
