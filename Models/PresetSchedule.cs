namespace DevLauncher.Models;

public enum ScheduleType
{
    None      = 0,
    OnStartup = 1,
    Once      = 2,
    Daily     = 3,
    Weekdays  = 4,
}

public class PresetSchedule
{
    public ScheduleType    Type      { get; set; } = ScheduleType.None;
    public bool            IsEnabled { get; set; } = true;
    public int             Hour      { get; set; } = 9;
    public int             Minute    { get; set; } = 0;
    public List<DayOfWeek> Days      { get; set; } = new();
    public DateTime?       RunOnceAt { get; set; }
    public DateTime?       LastRunAt { get; set; }

    public bool IsScheduled => Type != ScheduleType.None && IsEnabled;

    public string Summary => Type switch
    {
        ScheduleType.OnStartup => "⚡ On startup",
        ScheduleType.Once      => RunOnceAt.HasValue
                                    ? $"Once  {RunOnceAt.Value:MMM dd  HH:mm}"
                                    : "Once (time not set)",
        ScheduleType.Daily     => $"Daily  {Hour:D2}:{Minute:D2}",
        ScheduleType.Weekdays  => Days.Count > 0
                                    ? $"{string.Join(" ", Days.OrderBy(d => (int)d).Select(d => d.ToString()[..3]))}  {Hour:D2}:{Minute:D2}"
                                    : "Weekdays (no days)",
        _                      => "No schedule",
    };

    public PresetSchedule Clone() => new()
    {
        Type      = Type,
        IsEnabled = IsEnabled,
        Hour      = Hour,
        Minute    = Minute,
        Days      = new List<DayOfWeek>(Days),
        RunOnceAt = RunOnceAt,
        LastRunAt = LastRunAt,
    };

    public void CopyFrom(PresetSchedule src)
    {
        Type      = src.Type;
        IsEnabled = src.IsEnabled;
        Hour      = src.Hour;
        Minute    = src.Minute;
        Days      = new List<DayOfWeek>(src.Days);
        RunOnceAt = src.RunOnceAt;
        LastRunAt = src.LastRunAt;
    }
}
