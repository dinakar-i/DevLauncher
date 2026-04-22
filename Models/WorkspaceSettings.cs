namespace DevLauncher.Models;

/// <summary>
/// Stored in %AppData%\DevLauncher\workspace.json
/// Contains your Firebase config (baked in at build) and the
/// user's current joined workspace code.
/// </summary>
public class WorkspaceSettings
{
    // ── Your Firebase project config (you fill these in) ──────────
    // Get from Firebase Console → Project Settings → Your Apps → Web App
    public string FirebaseDatabaseUrl { get; set; } = string.Empty; // e.g. https://your-project-default-rtdb.firebaseio.com
    public string FirebaseApiKey      { get; set; } = string.Empty; // Web API Key

    // ── Per-user state ────────────────────────────────────────────
    public string WorkspaceCode   { get; set; } = string.Empty; // e.g. ACME-2024
    public string WorkspaceName   { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty; // shown in "last edited by"
    public bool   IsJoined        { get; set; } = false;
    public bool   AutoSync        { get; set; } = true;
}
