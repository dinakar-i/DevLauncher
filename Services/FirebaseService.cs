using WinForms = System.Windows.Forms;
using System.IO;
using System.Net.Http;
using System.Text;
using DevLauncher.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;

namespace DevLauncher.Services;

/// <summary>
/// Handles all Firebase Realtime Database operations for team preset sharing.
///
/// Database structure:
/// /workspaces/{workspaceCode}/
///     name:        "Acme Corp"
///     createdAt:   "2024-01-01T00:00:00Z"
///     presets/
///         {presetId}/   ← TeamPreset object
///
/// Security rules (set in Firebase Console):
/// {
///   "rules": {
///     "workspaces": {
///       "$code": {
///         ".read":  true,
///         ".write": true
///       }
///     }
///   }
/// }
/// </summary>
public class FirebaseService : IDisposable
{
    // ── YOUR FIREBASE CONFIG ──────────────────────────────────────
    // Replace these with your actual values from:
    // Firebase Console → Project Settings → General → Your Apps → Web App
    private const string DatabaseUrl = "https://postigo-development-default-rtdb.asia-southeast1.firebasedatabase.app";
    private const string ApiKey      = "AIzaSyC78zkb-jWvsRWyLUvIo6lrIEvNgvI7q4s";
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns false if the developer has not yet replaced the placeholder
    /// config values. The app will still open — just without Firebase features.
    /// </summary>
    public static bool IsConfigured =>
        !DatabaseUrl.Contains("YOUR-PROJECT") &&
        !ApiKey.Contains("YOUR-WEB-API-KEY");

    private FirebaseClient?          _client;
    private IDisposable?             _subscription;
    private string                   _currentWorkspaceCode = string.Empty;

    public bool  IsConnected   { get; private set; }
    public string StatusMessage { get; private set; } = "Not connected";

    public event Action<List<TeamPreset>>? PresetsUpdated;
    public event Action<string>?           ConnectionStatusChanged;

    // ── Connect ──────────────────────────────────────────────────

    public async Task<bool> ConnectAsync()
    {
        try
        {
            _client = new FirebaseClient(DatabaseUrl, new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult<string>(null!)
            });

            // Simple connectivity test
            await _client.Child("ping").OnceSingleAsync<object>();
            IsConnected   = true;
            StatusMessage = "Connected to Firebase";
            NotifyStatus();
            return true;
        }
        catch (Exception ex)
        {
            IsConnected   = false;
            StatusMessage = $"Connection failed: {ex.Message}";
            NotifyStatus();
            return false;
        }
    }

    // ── Workspace operations ──────────────────────────────────────

    /// <summary>
    /// Creates a new workspace node in Firebase.
    /// Safe to call even if workspace already exists — just overwrites the name/createdAt.
    /// </summary>
    public async Task CreateWorkspaceAsync(string code, string name)
    {
        EnsureConnected();
        code = code.ToUpperInvariant().Trim();

        var info = new
        {
            name      = name,
            createdAt = DateTime.UtcNow.ToString("o"),
            createdBy = "DevLauncher"
        };

        await _client!
            .Child("workspaces")
            .Child(code)
            .Child("info")
            .PutAsync(info);
    }

    /// <summary>Check if a workspace with this code exists in Firebase.</summary>
    public async Task<(bool exists, string name)> CheckWorkspaceAsync(string code)
    {
        EnsureConnected();
        try
        {
            var info = await _client!
                .Child("workspaces")
                .Child(code.ToUpperInvariant()).Child("info")
                .Child("name")
                .OnceSingleAsync<string>();

            return (info != null, info ?? string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// Join a workspace by code. Starts a real-time listener so
    /// any change in Firebase is pushed to the app immediately.
    /// </summary>
    public async Task<bool> JoinWorkspaceAsync(string code)
    {
        EnsureConnected();
        code = code.ToUpperInvariant().Trim();

        var (exists, name) = await CheckWorkspaceAsync(code);
        if (!exists)
        {
            StatusMessage = $"Workspace '{code}' not found.";
            NotifyStatus();
            return false;
        }

        // Stop any existing subscription
        _subscription?.Dispose();
        _currentWorkspaceCode = code;

        // Subscribe to real-time changes on the presets node
        _subscription = _client!
            .Child("workspaces")
            .Child(code)
            .Child("presets")
            .AsObservable<TeamPreset>()
            .Subscribe(
                onNext:  evt  => OnFirebaseEvent(evt),
                onError: err  => { StatusMessage = $"Sync error: {err.Message}"; NotifyStatus(); }
            );

        StatusMessage = $"Joined: {name} ({code})";
        NotifyStatus();
        return true;
    }

    public void LeaveWorkspace()
    {
        _subscription?.Dispose();
        _subscription            = null;
        _currentWorkspaceCode    = string.Empty;
        StatusMessage            = "Left workspace";
        NotifyStatus();
    }

    // ── Preset CRUD ───────────────────────────────────────────────

    /// <summary>Push a preset to Firebase (create or update).</summary>
    public async Task PushPresetAsync(LaunchPreset preset, string editedBy)
    {
        EnsureConnected();
        EnsureJoined();

        var teamPreset = TeamPreset.FromLaunchPreset(preset, editedBy);

        await _client!
            .Child("workspaces")
            .Child(_currentWorkspaceCode)
            .Child("presets")
            .Child(teamPreset.Id)
            .PutAsync(teamPreset);
    }

    /// <summary>Delete a preset from Firebase.</summary>
    public async Task DeletePresetAsync(string presetId)
    {
        EnsureConnected();
        EnsureJoined();

        await _client!
            .Child("workspaces")
            .Child(_currentWorkspaceCode)
            .Child("presets")
            .Child(presetId)
            .DeleteAsync();
    }

    /// <summary>Fetch all presets once (no live listener).</summary>
    public async Task<List<TeamPreset>> FetchAllPresetsAsync()
    {
        EnsureConnected();
        EnsureJoined();

        var result = await _client!
            .Child("workspaces")
            .Child(_currentWorkspaceCode)
            .Child("presets")
            .OnceAsync<TeamPreset>();

        return result?.Select(r => r.Object).ToList() ?? new();
    }

    // ── Private ───────────────────────────────────────────────────

    private void OnFirebaseEvent(Firebase.Database.Streaming.FirebaseEvent<TeamPreset> evt)
    {
        // Re-fetch full list so UI always has the complete picture
        _ = Task.Run(async () =>
        {
            try
            {
                var all = await FetchAllPresetsAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    PresetsUpdated?.Invoke(all));
            }
            catch { }
        });
    }

    private void EnsureConnected()
    {
        if (_client == null)
            throw new InvalidOperationException("Firebase not connected. Call ConnectAsync() first.");
    }

    private void EnsureJoined()
    {
        if (string.IsNullOrWhiteSpace(_currentWorkspaceCode))
            throw new InvalidOperationException("Not joined to any workspace.");
    }

    private void NotifyStatus()
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            ConnectionStatusChanged?.Invoke(StatusMessage));
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _client?.Dispose();
    }
}