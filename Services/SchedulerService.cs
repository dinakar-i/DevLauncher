using System.Windows.Threading;
using DevLauncher.Models;

namespace DevLauncher.Services;

public class SchedulerService
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(30) };

    private Func<IEnumerable<LaunchPreset>>? _getPresets;
    private Action<LaunchPreset>?            _launch;

    public void Start(Func<IEnumerable<LaunchPreset>> getPresets, Action<LaunchPreset> launch)
    {
        _getPresets = getPresets;
        _launch     = launch;
        _timer.Tick += (_, _) => Check(DateTime.Now);
        _timer.Start();
    }

    // Call once after startup delay to fire any OnStartup presets
    public void RunStartupPresets()
    {
        if (_getPresets == null || _launch == null) return;
        foreach (var p in _getPresets().ToList())
        {
            if (p.Schedule.IsEnabled && p.Schedule.Type == ScheduleType.OnStartup)
            {
                p.Schedule.LastRunAt = DateTime.Now;
                _launch(p);
            }
        }
    }

    private void Check(DateTime now)
    {
        if (_getPresets == null || _launch == null) return;
        foreach (var p in _getPresets().ToList())
        {
            if (ShouldRun(p.Schedule, now))
            {
                p.Schedule.LastRunAt = DateTime.Now;
                _launch(p);
            }
        }
    }

    public static bool ShouldRun(PresetSchedule s, DateTime now)
    {
        if (!s.IsEnabled || s.Type is ScheduleType.None or ScheduleType.OnStartup)
            return false;

        var current   = new TimeSpan(now.Hour, now.Minute, 0);
        var scheduled = new TimeSpan(s.Hour, s.Minute, 0);

        return s.Type switch
        {
            ScheduleType.Once =>
                s.RunOnceAt.HasValue
                && now >= s.RunOnceAt.Value
                && now < s.RunOnceAt.Value.AddMinutes(1)
                && (s.LastRunAt == null || s.LastRunAt < s.RunOnceAt),

            ScheduleType.Daily =>
                current == scheduled
                && (s.LastRunAt == null || s.LastRunAt.Value.Date < now.Date),

            ScheduleType.Weekdays =>
                s.Days.Contains(now.DayOfWeek)
                && current == scheduled
                && (s.LastRunAt == null || s.LastRunAt.Value.Date < now.Date),

            _ => false,
        };
    }

    // Returns a human-readable description of when this schedule fires next
    public static string NextRunText(PresetSchedule s)
    {
        if (!s.IsEnabled || s.Type == ScheduleType.None) return "—";
        if (s.Type == ScheduleType.OnStartup)            return "Next app launch";

        var now  = DateTime.Now;
        var time = new TimeSpan(s.Hour, s.Minute, 0);

        switch (s.Type)
        {
            case ScheduleType.Once:
                if (!s.RunOnceAt.HasValue) return "Date not set";
                return s.RunOnceAt.Value > now
                    ? s.RunOnceAt.Value.ToString("ddd MMM dd  HH:mm")
                    : "Already passed";

            case ScheduleType.Daily:
                var todayRun = now.Date + time;
                var next     = todayRun > now ? todayRun : todayRun.AddDays(1);
                return next.ToString("ddd MMM dd  HH:mm");

            case ScheduleType.Weekdays:
                if (s.Days.Count == 0) return "No days selected";
                for (int i = 1; i <= 7; i++)
                {
                    var candidate = now.Date.AddDays(i);
                    if (!s.Days.Contains(candidate.DayOfWeek)) continue;
                    var run = candidate + time;
                    if (i == 0 && run <= now) continue;
                    return run.ToString("ddd MMM dd  HH:mm");
                }
                // same-day check
                var todayWeekday = now.Date + time;
                if (s.Days.Contains(now.DayOfWeek) && todayWeekday > now)
                    return todayWeekday.ToString("ddd MMM dd  HH:mm");
                return "—";

            default:
                return "—";
        }
    }
}
