using System.Windows;
using System.Windows.Input;
using DevLauncher.Models;
using DevLauncher.Services;

using WpfBrush      = System.Windows.Media.Brush;
using WpfSolidBrush = System.Windows.Media.SolidColorBrush;
using WpfColors     = System.Windows.Media.Colors;
using WpfBorder     = System.Windows.Controls.Border;
using WpfTextBlock  = System.Windows.Controls.TextBlock;
using WpfCanvas     = System.Windows.Controls.Canvas;

namespace DevLauncher.Views;

public partial class ScheduleDialog : Window
{
    private readonly LaunchPreset   _preset;
    private readonly PresetSchedule _working;   // edited copy; written to preset only on Save

    public ScheduleDialog(LaunchPreset preset)
    {
        InitializeComponent();
        _preset  = preset;
        _working = preset.Schedule.Clone();

        TitlePresetName.Text = preset.Name;
        LoadFromWorking();
    }

    // ── Load ─────────────────────────────────────────────────────────

    private void LoadFromWorking()
    {
        HourDisplay.Text   = _working.Hour.ToString("D2");
        MinuteDisplay.Text = _working.Minute.ToString("D2");
        ScheduleDatePicker.SelectedDate = _working.RunOnceAt?.Date ?? DateTime.Today.AddDays(1);

        SetTypeActive(_working.Type);
        RefreshDayButtons();
        RefreshToggle();
        RefreshNextRun();
    }

    // ── Type pills ───────────────────────────────────────────────────

    private void TypeNone_Click(object s, MouseButtonEventArgs e)     => SetTypeActive(ScheduleType.None);
    private void TypeStartup_Click(object s, MouseButtonEventArgs e)  => SetTypeActive(ScheduleType.OnStartup);
    private void TypeOnce_Click(object s, MouseButtonEventArgs e)     => SetTypeActive(ScheduleType.Once);
    private void TypeDaily_Click(object s, MouseButtonEventArgs e)    => SetTypeActive(ScheduleType.Daily);
    private void TypeWeekdays_Click(object s, MouseButtonEventArgs e) => SetTypeActive(ScheduleType.Weekdays);

    private void SetTypeActive(ScheduleType type)
    {
        _working.Type = type;

        var accent = (WpfBrush)FindResource("AccentGradient");
        var transp = new WpfSolidBrush(WpfColors.Transparent);
        var white  = new WpfSolidBrush(WpfColors.White);
        var muted  = (WpfBrush)FindResource("Muted2Brush");

        (WpfBorder tab, WpfTextBlock txt, ScheduleType t)[] pills =
        {
            (TypeNone,     TypeNoneText,     ScheduleType.None),
            (TypeStartup,  TypeStartupText,  ScheduleType.OnStartup),
            (TypeOnce,     TypeOnceText,     ScheduleType.Once),
            (TypeDaily,    TypeDailyText,    ScheduleType.Daily),
            (TypeWeekdays, TypeWeekdaysText, ScheduleType.Weekdays),
        };

        foreach (var (tab, txt, t) in pills)
        {
            bool active    = t == type;
            tab.Background = active ? accent : transp;
            txt.Foreground = active ? white  : muted;
        }

        bool hasTime = type is ScheduleType.Daily or ScheduleType.Weekdays or ScheduleType.Once;
        TimeSection.Visibility  = hasTime                       ? Visibility.Visible : Visibility.Collapsed;
        DateSection.Visibility  = type == ScheduleType.Once     ? Visibility.Visible : Visibility.Collapsed;
        DaysSection.Visibility  = type == ScheduleType.Weekdays ? Visibility.Visible : Visibility.Collapsed;

        RefreshNextRun();
    }

    // ── Time spinners ────────────────────────────────────────────────

    private void HourUp_Click(object sender, RoutedEventArgs e)
    {
        _working.Hour      = (_working.Hour + 1) % 24;
        HourDisplay.Text   = _working.Hour.ToString("D2");
        SyncRunOnceTime();
        RefreshNextRun();
    }

    private void HourDown_Click(object sender, RoutedEventArgs e)
    {
        _working.Hour      = (_working.Hour + 23) % 24;
        HourDisplay.Text   = _working.Hour.ToString("D2");
        SyncRunOnceTime();
        RefreshNextRun();
    }

    private void MinuteUp_Click(object sender, RoutedEventArgs e)
    {
        _working.Minute    = (_working.Minute + 1) % 60;
        MinuteDisplay.Text = _working.Minute.ToString("D2");
        SyncRunOnceTime();
        RefreshNextRun();
    }

    private void MinuteDown_Click(object sender, RoutedEventArgs e)
    {
        _working.Minute    = (_working.Minute + 59) % 60;
        MinuteDisplay.Text = _working.Minute.ToString("D2");
        SyncRunOnceTime();
        RefreshNextRun();
    }

    // ── Date picker ──────────────────────────────────────────────────

    private void DatePicker_SelectedDateChanged(object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_working == null) return;
        SyncRunOnceTime();
        RefreshNextRun();
    }

    // Keeps RunOnceAt in sync whenever date or time changes
    private void SyncRunOnceTime()
    {
        if (ScheduleDatePicker.SelectedDate.HasValue)
            _working.RunOnceAt = ScheduleDatePicker.SelectedDate.Value.Date
                                 + new TimeSpan(_working.Hour, _working.Minute, 0);
    }

    // ── Day toggles ──────────────────────────────────────────────────

    private void Day_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not WpfBorder b || !int.TryParse(b.Tag?.ToString(), out int tagVal)) return;
        var day = (DayOfWeek)tagVal;
        if (_working.Days.Contains(day)) _working.Days.Remove(day);
        else                             _working.Days.Add(day);
        RefreshDayButtons();
        RefreshNextRun();
    }

    private void RefreshDayButtons()
    {
        var accent  = (WpfBrush)FindResource("AccentGradient");
        var surface = (WpfBrush)FindResource("SurfaceBrush");
        var border  = (WpfBrush)FindResource("BorderBrush");
        var accentB = (WpfBrush)FindResource("AccentBrush");
        var muted   = (WpfBrush)FindResource("Muted2Brush");

        (WpfBorder b, DayOfWeek day)[] days =
        {
            (DayMon, DayOfWeek.Monday),    (DayTue, DayOfWeek.Tuesday),
            (DayWed, DayOfWeek.Wednesday), (DayThu, DayOfWeek.Thursday),
            (DayFri, DayOfWeek.Friday),    (DaySat, DayOfWeek.Saturday),
            (DaySun, DayOfWeek.Sunday),
        };

        foreach (var (btn, day) in days)
        {
            bool on        = _working.Days.Contains(day);
            btn.Background  = on ? accent  : surface;
            btn.BorderBrush = on ? accentB : border;
            var tb = (WpfTextBlock)btn.Child;
            tb.Foreground   = on ? new WpfSolidBrush(WpfColors.White) : muted;
        }
    }

    // ── Enable toggle ────────────────────────────────────────────────

    private void EnableToggle_Click(object sender, MouseButtonEventArgs e)
    {
        _working.IsEnabled = !_working.IsEnabled;
        RefreshToggle();
        RefreshNextRun();
    }

    private void RefreshToggle()
    {
        bool on = _working.IsEnabled;
        EnableToggle.Background = on
            ? (WpfBrush)FindResource("Accent3Brush")
            : (WpfBrush)FindResource("MutedBrush");
        EnableThumb.Fill = new WpfSolidBrush(WpfColors.White);
        WpfCanvas.SetLeft(EnableThumb, on ? 24 : 4);
    }

    // ── Next run label ───────────────────────────────────────────────

    private void RefreshNextRun()
        => NextRunLabel.Text = SchedulerService.NextRunText(_working);

    // ── Window chrome ────────────────────────────────────────────────

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void CloseBtn_Click(object sender, MouseButtonEventArgs e) => CancelAndClose();

    // ── Save / Cancel ────────────────────────────────────────────────

    private void Cancel_Click(object sender, RoutedEventArgs e) => CancelAndClose();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _preset.Schedule.CopyFrom(_working);
        DialogResult = true;
        Close();
    }

    private void CancelAndClose()
    {
        DialogResult = false;
        Close();
    }
}
