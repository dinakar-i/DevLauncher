using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using DevLauncher.Models;

using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace DevLauncher.Views;

public partial class ScheduleDialog : Window
{
    private readonly LaunchPreset _preset;

    public ObservableCollection<ScheduleEditItem> Items { get; } = new();

    public IRelayCommand<ScheduleEditItem> DeleteItemCommand { get; }

    public ScheduleDialog(LaunchPreset preset)
    {
        _preset = preset;
        DeleteItemCommand = new RelayCommand<ScheduleEditItem>(item => Items.Remove(item!));

        InitializeComponent();
        DataContext = this;

        TitlePresetName.Text = preset.Name;

        foreach (var s in preset.Schedules)
            Items.Add(ScheduleEditItem.From(s));

        if (Items.Count == 0)
            Items.Add(new ScheduleEditItem());
    }

    // ── Row toggle (the pill switch on each card) ─────────────────────
    private void RowToggle_Click(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ScheduleEditItem item)
            item.IsEnabled = !item.IsEnabled;
    }

    // ── Add new schedule row ──────────────────────────────────────────
    private void AddSchedule_Click(object sender, RoutedEventArgs e)
        => Items.Add(new ScheduleEditItem());

    // ── Window chrome ─────────────────────────────────────────────────
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void CloseBtn_Click(object sender, MouseButtonEventArgs e) => Cancel();

    // ── Save / Cancel ─────────────────────────────────────────────────
    private void Cancel_Click(object sender, RoutedEventArgs e) => Cancel();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _preset.Schedules = Items
            .Where(i => i.Type != ScheduleType.None)
            .Select(i => i.ToPresetSchedule())
            .ToList();
        DialogResult = true;
        Close();
    }

    private void Cancel()
    {
        DialogResult = false;
        Close();
    }
}
