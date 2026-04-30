using WinForms = System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using DevLauncher.Models;

namespace DevLauncher.Services;

public class ProcessExecutionService
{
    /// <summary>
    /// Launches all commands of a preset into the given TerminalSession.
    /// Runs entirely on a background thread — caller awaits it.
    /// </summary>
    public async Task RunSessionAsync(
        LaunchPreset     preset,
        TerminalSession  session,
        Action           onSessionUpdated,
        CancellationToken ct)
    {
        void Emit(string text, TerminalLineType type)
        {
            var line = new TerminalLine { Text = text, Type = type };
            // Always dispatch to UI thread — ObservableCollection requires it
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                session.Lines.Add(line);
                while (session.Lines.Count > 2000)
                    session.Lines.RemoveAt(0);
                onSessionUpdated();
            });
        }

        Emit("══════════════════════════════════════════", TerminalLineType.Separator);
        Emit($"  {preset.Icon}  {preset.Name}", TerminalLineType.Info);
        Emit($"  Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", TerminalLineType.Info);
        if (!string.IsNullOrWhiteSpace(preset.WorkingDirectory))
            Emit($"  Dir: {ResolveDirectory(preset.WorkingDirectory)}", TerminalLineType.Info);
        Emit("══════════════════════════════════════════", TerminalLineType.Separator);

        try
        {
            foreach (var cmd in preset.Commands)
            {
                if (ct.IsCancellationRequested) break;

                if (!string.IsNullOrWhiteSpace(cmd.Description))
                    Emit($"  → {cmd.Description}", TerminalLineType.Info);

                Emit($"$ {cmd.Command}", TerminalLineType.Command);

                bool ok = await RunCommandAsync(cmd, preset.WorkingDirectory, session, Emit, ct);

                if (!ok && !cmd.RunInBackground)
                {
                    session.HasError = true;
                    Emit($"⚠  Command failed — aborting preset.", TerminalLineType.Warning);
                    return;
                }

                if (cmd.DelayAfterMs > 0)
                    await Task.Delay(cmd.DelayAfterMs, ct);
            }

            if (!string.IsNullOrWhiteSpace(preset.OpenUrlAfterLaunch) && !ct.IsCancellationRequested)
            {
                Emit($"  Opening {preset.OpenUrlAfterLaunch}", TerminalLineType.Info);
                OpenUrl(preset.OpenUrlAfterLaunch);
            }

            Emit("══════════════════════════════════════════", TerminalLineType.Separator);
            Emit($"  ✓ All commands launched — session is live.", TerminalLineType.Success);
            Emit("══════════════════════════════════════════", TerminalLineType.Separator);
        }
        catch (OperationCanceledException)
        {
            Emit("  ✗ Session cancelled.", TerminalLineType.Warning);
        }
        catch (Exception ex)
        {
            session.HasError = true;
            Emit($"  ✗ Unexpected error: {ex.Message}", TerminalLineType.Error);
        }
        finally
        {
            session.IsRunning = false;
            System.Windows.Application.Current.Dispatcher.Invoke(onSessionUpdated);
        }
    }

    private async Task<bool> RunCommandAsync(
        LaunchCommand   cmd,
        string          workingDir,
        TerminalSession session,
        Action<string, TerminalLineType> emit,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "cmd.exe",
            Arguments              = $"/c {cmd.Command}",
            WorkingDirectory       = ResolveDirectory(workingDir),
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding  = System.Text.Encoding.UTF8,
        };

        Process process;
        try
        {
            process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.Start();
        }
        catch (Exception ex)
        {
            emit($"  ERROR: {ex.Message}", TerminalLineType.Error);
            return false;
        }

        session.Processes.Add(process);

        // Stream stdout + stderr live
        var stdoutTask = StreamAsync(process.StandardOutput, TerminalLineType.Output, emit, ct);
        var stderrTask = StreamAsync(process.StandardError,  TerminalLineType.Error,  emit, ct);

        if (cmd.RunInBackground)
        {
            emit($"  ↑ Running in background (PID {process.Id})", TerminalLineType.Info);
            // Don't await — let it keep streaming while next command starts
            _ = Task.WhenAll(stdoutTask, stderrTask);
            return true;
        }

        // Foreground: wait for exit
        await Task.WhenAll(stdoutTask, stderrTask);
        try { await process.WaitForExitAsync(ct); } catch { }

        bool success = process.ExitCode == 0;
        emit(success
            ? $"  ✓ Exit code 0"
            : $"  ✗ Exit code {process.ExitCode}",
            success ? TerminalLineType.Success : TerminalLineType.Error);

        return success;
    }

    private static async Task StreamAsync(
        StreamReader reader,
        TerminalLineType type,
        Action<string, TerminalLineType> emit,
        CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;
                if (!string.IsNullOrEmpty(line))
                    emit(line, type);
            }
        }
        catch { /* stream closed or cancelled */ }
    }

    /// <summary>
    /// Starts a persistent cmd.exe shell for the session. Keeps running until the session
    /// is killed — commands sent via session.ShellWriter.WriteLine() execute in-place,
    /// preserving directory, env vars, and other shell state across calls.
    /// </summary>
    public async Task StartInteractiveShellAsync(
        TerminalSession session,
        CancellationToken ct,
        Action? onShellReady      = null,
        Action? onPromptDetected  = null)
    {
        void Emit(string text, TerminalLineType type)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                session.Lines.Add(new TerminalLine { Text = text, Type = type });
                while (session.Lines.Count > 2000)
                    session.Lines.RemoveAt(0);
            });
        }

        var psi = new ProcessStartInfo
        {
            FileName               = "cmd.exe",
            // /Q = quiet (suppress command echo — we echo it ourselves styled as Command)
            // /D = skip registry AUTORUN keys for faster startup
            Arguments              = "/Q /D",
            WorkingDirectory       = ResolveDirectory(session.WorkingDirectory),
            UseShellExecute        = false,
            RedirectStandardInput  = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding  = System.Text.Encoding.UTF8,
        };

        // $P$G$_ = "path>" + CR+LF — the trailing newline lets ReadLineAsync capture the
        // prompt as a complete line instead of blocking until the next command runs.
        psi.Environment["PROMPT"] = "$P$G$_";

        Process process;
        try
        {
            process = new Process { StartInfo = psi };
            process.Start();
        }
        catch (Exception ex)
        {
            Emit($"  ✗ Shell failed to start: {ex.Message}", TerminalLineType.Error);
            return;
        }

        session.ShellWriter           = process.StandardInput;
        session.ShellWriter.AutoFlush = true;
        session.Processes.Add(process);
        onShellReady?.Invoke();

        var stdoutTask = StreamShellStdoutAsync(process.StandardOutput, Emit, onPromptDetected, ct);
        var stderrTask = StreamAsync(process.StandardError, TerminalLineType.Error, Emit, ct);

        await Task.WhenAll(stdoutTask, stderrTask);
        try { await process.WaitForExitAsync(ct); } catch { }

        session.ShellWriter = null;
    }

    // Streams the shell's stdout and fires onPromptDetected whenever the shell emits
    // its next prompt (a line ending with '>'), indicating a command has finished.
    private static async Task StreamShellStdoutAsync(
        StreamReader reader,
        Action<string, TerminalLineType> emit,
        Action? onPromptDetected,
        CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break;
                if (!string.IsNullOrEmpty(line))
                {
                    emit(line, TerminalLineType.Output);
                    // cmd.exe prompt ($P$G$_) always ends with '>'
                    if (line.TrimEnd().EndsWith('>'))
                        onPromptDetected?.Invoke();
                }
            }
        }
        catch { /* stream closed or cancelled */ }
    }

    public void KillSession(TerminalSession session)
    {
        foreach (var p in session.Processes)
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
        }
        session.IsRunning = false;
    }

    private static string ResolveDirectory(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir)) return Directory.GetCurrentDirectory();
        dir = dir.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        return Directory.Exists(dir) ? dir : Directory.GetCurrentDirectory();
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }
}