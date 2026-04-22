using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DevLauncher.Models;
using DevLauncher.ViewModels;

// Suppress WinForms ambiguity globally for this file
using WinForms = System.Windows.Forms;

namespace DevLauncher.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = (MainViewModel)DataContext;

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_vm.ActiveSession))
                SubscribeToActiveSession();
        };

        StateChanged += (_, _) => UpdateMaximizeIcon();
        SetActiveTab("Presets");
    }

    // ─── Tab switching ─────────────────────────────────────────────
    private void Tab_Presets_Click(object sender, MouseButtonEventArgs e) => SetActiveTab("Presets");
    private void Tab_Custom_Click(object sender, MouseButtonEventArgs e)  => SetActiveTab("Custom");
    private void Tab_Team_Click(object sender, MouseButtonEventArgs e)    => SetActiveTab("Team");

    private void SetActiveTab(string tab)
    {
        PresetsPanel.Visibility = tab == "Presets" ? Visibility.Visible : Visibility.Collapsed;
        CustomPanel.Visibility  = tab == "Custom"  ? Visibility.Visible : Visibility.Collapsed;
        TeamPanel.Visibility    = tab == "Team"    ? Visibility.Visible : Visibility.Collapsed;

        var accent      = (LinearGradientBrush)FindResource("AccentGradient");
        var transparent = new SolidColorBrush(Colors.Transparent);
        var white       = new SolidColorBrush(Colors.White);
        var muted       = (SolidColorBrush)FindResource("Muted2Brush");

        TabPresetsHeader.Background = tab == "Presets" ? accent      : transparent;
        TabCustomHeader.Background  = tab == "Custom"  ? accent      : transparent;
        TabTeamHeader.Background    = tab == "Team"    ? accent      : transparent;

        TabPresetsText.Foreground   = tab == "Presets" ? white : muted;
        TabCustomText.Foreground    = tab == "Custom"  ? white : muted;
        TabTeamText.Foreground      = tab == "Team"    ? white : muted;
    }

    // ─── Session clicks ────────────────────────────────────────────
    private void SessionTab_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is TerminalSession session)
        {
            _vm.ActiveSession = session;
            ScrollTerminalToBottom();
        }
    }

    private void SessionSidebarItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.DataContext is TerminalSession session)
        {
            _vm.ActiveSession = session;
            ScrollTerminalToBottom();
        }
    }

    // ─── Workspace Create/Join tab switcher ──────────────────────

    private void WsTab_Create_Click(object sender, MouseButtonEventArgs e) => SetWsTab("Create");
    private void WsTab_Join_Click(object sender, MouseButtonEventArgs e)   => SetWsTab("Join");

    private void SetWsTab(string tab)
    {
        bool create = tab == "Create";

        WsCreatePanel.Visibility = create ? Visibility.Visible   : Visibility.Collapsed;
        WsJoinPanel.Visibility   = create ? Visibility.Collapsed : Visibility.Visible;

        var accentBrush     = (System.Windows.Media.LinearGradientBrush)FindResource("AccentGradient");
        var transparentBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
        var whiteBrush      = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        var mutedBrush      = (System.Windows.Media.SolidColorBrush)FindResource("Muted2Brush");

        WsCreateTab.Background = create ? accentBrush      : transparentBrush;
        WsJoinTab.Background   = create ? transparentBrush : accentBrush;
        WsCreateTabText.Foreground = create ? whiteBrush : mutedBrush;
        WsJoinTabText.Foreground   = create ? mutedBrush : whiteBrush;
    }

    // ─── Directory Browser ─────────────────────────────────────────
    private void BrowseNewPresetDir_Click(object sender, RoutedEventArgs e)
    {
        var path = MainViewModel.BrowseFolder(_vm.NewPresetWorkingDir);
        if (path != null) _vm.NewPresetWorkingDir = path;
    }

    // ─── Icon Picker ───────────────────────────────────────────────
    private void IconPickerBtn_Click(object sender, RoutedEventArgs e)
    {
        IconPickerControl.IconSelected -= OnIconSelected;
        IconPickerControl.IconSelected += OnIconSelected;
        IconPickerPopup.IsOpen = true;
    }

    private void OnIconSelected(string icon)
    {
        _vm.NewPresetIcon = icon;
        IconPickerPopup.IsOpen = false;
    }

    // ─── Auto-scroll terminal ──────────────────────────────────────
    private TerminalSession? _subscribedSession;

    private void SubscribeToActiveSession()
    {
        if (_subscribedSession != null)
            _subscribedSession.Lines.CollectionChanged -= OnLinesChanged;
        _subscribedSession = _vm.ActiveSession;
        if (_subscribedSession != null)
        {
            _subscribedSession.Lines.CollectionChanged += OnLinesChanged;
            ScrollTerminalToBottom();
        }
    }

    private void OnLinesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => Dispatcher.BeginInvoke(ScrollTerminalToBottom,
               System.Windows.Threading.DispatcherPriority.Background);

    private void ScrollTerminalToBottom() => TerminalScroll.ScrollToBottom();

    // ─── Window chrome ─────────────────────────────────────────────
    private void Minimize_Click(object sender, MouseButtonEventArgs e)
        => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, MouseButtonEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
               ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, MouseButtonEventArgs e)
    {
        _vm.CloseAllSessionsCommand.Execute(null);
        Close();
    }

    private void UpdateMaximizeIcon()
        => MaximizeIcon.Text = WindowState == WindowState.Maximized ? "\u2752" : "\u2610";
}