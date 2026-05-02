using System.Windows.Threading;
using DevLauncher.Models;

namespace DevLauncher.Services;

public class SchedulerService
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(15) };

    private Func<IEnumerable<LaunchPreset>>? _getPresets;
    private Action<LaunchPreset>?            _launch;
    private Action?                          _savePresets;

    public void Start(
        Func<IEnumerable<LaunchPreset>> getPresets,
        Action<LaunchPreset>            launch,
        Action?                         savePresets = null)
    {
        _getPresets  = getPresets;
        _launch      = launch;
        _savePresets = savePresets;
        _timer.Tick += (_, _) => Check(DateTime.Now);
        _timer.Start();
    }

    public void RunStartupPresets()
    {
        if (_getPresets == null || _launch == null) return;
        bool any = false;
        foreach (var p in _getPresets().ToList())
        {
            var startup = p.Schedules.Where(s => s.IsEnabled && s.Type == ScheduleType.OnStartup).ToList();
            if (startup.Count == 0) continue;
            foreach (var s in startup) s.LastRunAt = DateTime.Now;
            _launch(p);
            any = true;
        }
        if (any) _savePresets?.Invoke();
    }

    private void Check(DateTime now)
    {
        if (_getPresets == null || _launch == null) return;
        bool any = false;
        foreach (var p in _getPresets().ToList())
        {
            foreach (var s in p.Schedules)
            {
                if (!ShouldRun(s, now)) continue;
                s.LastRunAt = DateTime.Now;
                _launch(p);
                any = true;
                break; // one fire per preset per tick even if multiple schedules match
            }
        }
        if (any) _savePresets?.Invoke();
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
                && now < s.RunOnceAt.Value.AddMinutes(2)
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
                    ? s.RunOnceAt.Value.ToString("ddd MMM dd  h:mm tt")
                    : "Already passed";

            case ScheduleType.Daily:
                var todayRun = now.Date + time;
                var next     = todayRun > now ? todayRun : todayRun.AddDays(1);
                return next.ToString("ddd MMM dd  h:mm tt");

            case ScheduleType.Weekdays:
                if (s.Days.Count == 0) return "No days selected";
                var todaySlot = now.Date + time;
                if (s.Days.Contains(now.DayOfWeek) && todaySlot > now)
                    return todaySlot.ToString("ddd MMM dd  h:mm tt");
                for (int i = 1; i <= 7; i++)
                {
                    var c = now.Date.AddDays(i);
                    if (s.Days.Contains(c.DayOfWeek))
                        return (c + time).ToString("ddd MMM dd  h:mm tt");
                }
                return "—";

            default: return "—";
        }
    }
}
