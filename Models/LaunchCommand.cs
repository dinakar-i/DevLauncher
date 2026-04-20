namespace DevLauncher.Models;

public class LaunchCommand
{
    public string Command { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RunInBackground { get; set; } = false;
    public int DelayAfterMs { get; set; } = 0;
}
