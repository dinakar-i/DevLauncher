using System.IO;
using DevLauncher.Models;
using Newtonsoft.Json;

namespace DevLauncher.Services;

public class WorkspaceStorageService
{
    private static readonly string AppDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevLauncher");

    private static readonly string SettingsFile =
        Path.Combine(AppDataDir, "workspace.json");

    public WorkspaceSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFile)) return new();
            var json = File.ReadAllText(SettingsFile);
            return JsonConvert.DeserializeObject<WorkspaceSettings>(json) ?? new();
        }
        catch { return new(); }
    }

    public void Save(WorkspaceSettings settings)
    {
        try
        {
            Directory.CreateDirectory(AppDataDir);
            File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
        catch { }
    }
}
