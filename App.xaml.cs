// Explicit WPF aliases — UseWindowsForms=true causes System.Windows.Forms
// to conflict with System.Windows types.
using WpfApp        = System.Windows.Application;
using WpfStartup    = System.Windows.StartupEventArgs;
using WinForms      = System.Windows.Forms;

using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace DevLauncher;

public partial class App : WpfApp
{
    protected override void OnStartup(WpfStartup e)
    {
        // ── Global exception handlers ─────────────────────────────
        // Catch ALL unhandled exceptions so the app never silently dies.
        // Errors are written to %AppData%\DevLauncher\crash.log

        DispatcherUnhandledException += OnDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;
        TaskScheduler.UnobservedTaskException += OnTaskException;

        base.OnStartup(e);
    }

    // ── Handlers ──────────────────────────────────────────────────

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash("UI Thread", e.Exception);
        ShowCrashDialog(e.Exception);
        e.Handled = true; // keep app alive if possible
    }

    private static void OnDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogCrash("AppDomain", ex);
    }

    private static void OnTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogCrash("Task", e.Exception);
        e.SetObserved(); // prevent process termination
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static void LogCrash(string source, Exception ex)
    {
        try
        {
            string dir  = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DevLauncher");
            Directory.CreateDirectory(dir);

            string log  = Path.Combine(dir, "crash.log");
            string entry = $"""
                ════════════════════════════════════
                [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}
                {ex.GetType().Name}: {ex.Message}
                {ex.StackTrace}

                """;
            File.AppendAllText(log, entry);
        }
        catch { /* logging must never throw */ }
    }

    private static void ShowCrashDialog(Exception ex)
    {
        string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevLauncher", "crash.log");

        System.Windows.MessageBox.Show(
            $"An error occurred:\n\n{ex.Message}\n\nDetails saved to:\n{logPath}",
            "DevLauncher — Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
