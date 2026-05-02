using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevLauncher.Models;

namespace DevLauncher.Views;

/// <summary>ViewModel for one schedule row inside the ScheduleDialog.</summary>
public partial class ScheduleEditItem : ObservableObject
{
    // ── Core fields ───────────────────────────────────────────────────
    [ObservableProperty] private ScheduleType _type        = ScheduleType.None;
    [ObservableProperty] private bool          _isEnabled   = true;
    [ObservableProperty] private int           _displayHour = 9;    // 1-12
    [ObservableProperty] private bool          _isAm        = true;
    [ObservableProperty] private int           _minute      = 0;
    [ObservableProperty] private DateTime?     _runOnceDate = DateTime.Today.AddDays(1);

    // ── Day flags ─────────────────────────────────────────────────────
    [ObservableProperty] private bool _isMon;
    [ObservableProperty] private bool _isTue;
    [ObservableProperty] private bool _isWed;
    [ObservableProperty] private bool _isThu;
    [ObservableProperty] private bool _isFri;
    [ObservableProperty] private bool _isSat;
    [ObservableProperty] private bool _isSun;

    // ── Derived display texts ─────────────────────────────────────────
    public string HourText   => DisplayHour.ToString("D2");
    public string MinuteText => Minute.ToString("D2");
    public string AmPmText   => IsAm ? "AM" : "PM";

    // ── Visibility helpers (notify when Type changes) ─────────────────
    public bool HasTime => Type is ScheduleType.Daily or ScheduleType.Weekdays or ScheduleType.Once;
    public bool HasDate => Type == ScheduleType.Once;
    public bool HasDays => Type == ScheduleType.Weekdays;

    partial void OnTypeChanged(ScheduleType value)
    {
        OnPropertyChanged(nameof(HasTime));
        OnPropertyChanged(nameof(HasDate));
        OnPropertyChanged(nameof(HasDays));
    }

    partial void OnDisplayHourChanged(int value)  => OnPropertyChanged(nameof(HourText));
    partial void OnIsAmChanged(bool value)         => OnPropertyChanged(nameof(AmPmText));
    partial void OnMinuteChanged(int value)        => OnPropertyChanged(nameof(MinuteText));

    // ── Type commands ─────────────────────────────────────────────────
    [RelayCommand] void SetTypeNone()     => Type = ScheduleType.None;
    [RelayCommand] void SetTypeStartup()  => Type = ScheduleType.OnStartup;
    [RelayCommand] void SetTypeOnce()     => Type = ScheduleType.Once;
    [RelayCommand] void SetTypeDaily()    => Type = ScheduleType.Daily;
    [RelayCommand] void SetTypeWeekdays() => Type = ScheduleType.Weekdays;

    // ── Time commands ─────────────────────────────────────────────────
    [RelayCommand] void HourUp()    => DisplayHour = DisplayHour == 12 ? 1  : DisplayHour + 1;
    [RelayCommand] void HourDown()  => DisplayHour = DisplayHour == 1  ? 12 : DisplayHour - 1;
    [RelayCommand] void MinuteUp()  => Minute = (Minute + 1)  % 60;
    [RelayCommand] void MinuteDown()=> Minute = (Minute + 59) % 60;
    [RelayCommand] void ToggleAmPm()=> IsAm = !IsAm;

    // ── Day toggle commands ───────────────────────────────────────────
    [RelayCommand] void ToggleMon() => IsMon = !IsMon;
    [RelayCommand] void ToggleTue() => IsTue = !IsTue;
    [RelayCommand] void ToggleWed() => IsWed = !IsWed;
    [RelayCommand] void ToggleThu() => IsThu = !IsThu;
    [RelayCommand] void ToggleFri() => IsFri = !IsFri;
    [RelayCommand] void ToggleSat() => IsSat = !IsSat;
    [RelayCommand] void ToggleSun() => IsSun = !IsSun;

    // ── 24-hour derived value ─────────────────────────────────────────
    public int Hour24 => IsAm
        ? (DisplayHour == 12 ? 0  : DisplayHour)
        : (DisplayHour == 12 ? 12 : DisplayHour + 12);

    // ── Factory / conversion ──────────────────────────────────────────
    public static ScheduleEditItem From(PresetSchedule s)
    {
        var item = new ScheduleEditItem
        {
            Type        = s.Type,
            IsEnabled   = s.IsEnabled,
            _minute     = s.Minute,
            RunOnceDate = s.RunOnceAt?.Date ?? DateTime.Today.AddDays(1),
            IsAm        = s.Hour < 12,
            DisplayHour = s.Hour % 12 == 0 ? 12 : s.Hour % 12,
            IsMon = s.Days.Contains(DayOfWeek.Monday),
            IsTue = s.Days.Contains(DayOfWeek.Tuesday),
            IsWed = s.Days.Contains(DayOfWeek.Wednesday),
            IsThu = s.Days.Contains(DayOfWeek.Thursday),
            IsFri = s.Days.Contains(DayOfWeek.Friday),
            IsSat = s.Days.Contains(DayOfWeek.Saturday),
            IsSun = s.Days.Contains(DayOfWeek.Sunday),
        };
        return item;
    }

    public PresetSchedule ToPresetSchedule()
    {
        var days = new List<DayOfWeek>();
        if (IsMon) days.Add(DayOfWeek.Monday);
        if (IsTue) days.Add(DayOfWeek.Tuesday);
        if (IsWed) days.Add(DayOfWeek.Wednesday);
        if (IsThu) days.Add(DayOfWeek.Thursday);
        if (IsFri) days.Add(DayOfWeek.Friday);
        if (IsSat) days.Add(DayOfWeek.Saturday);
        if (IsSun) days.Add(DayOfWeek.Sunday);

        return new PresetSchedule
        {
            Type      = Type,
            IsEnabled = IsEnabled,
            Hour      = Hour24,
            Minute    = Minute,
            Days      = days,
            RunOnceAt = HasDate
                ? RunOnceDate?.Date + new TimeSpan(Hour24, Minute, 0)
                : null,
        };
    }
}
