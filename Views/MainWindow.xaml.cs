using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DevLauncher.Models;
using DevLauncher.ViewModels;

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

    private void SetActiveTab(string tab)
    {
        bool presets = tab == "Presets";
        PresetsPanel.Visibility = presets ? Visibility.Visible   : Visibility.Collapsed;
        CustomPanel.Visibility  = presets ? Visibility.Collapsed : Visibility.Visible;

        var accentBrush     = (LinearGradientBrush)FindResource("AccentGradient");
        var transparentBrush = new SolidColorBrush(Colors.Transparent);
        var whiteBrush      = new SolidColorBrush(Colors.White);
        var mutedBrush      = (SolidColorBrush)FindResource("Muted2Brush");

        TabPresetsHeader.Background = presets ? accentBrush      : transparentBrush;
        TabCustomHeader.Background  = presets ? transparentBrush : accentBrush;
        TabPresetsText.Foreground   = presets ? whiteBrush       : mutedBrush;
        TabCustomText.Foreground    = presets ? mutedBrush       : whiteBrush;
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

    // ─── Auto-scroll ───────────────────────────────────────────────
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

    // ─── Directory Browser ────────────────────────────────────────

    // Called from the Browse button in the Custom preset builder
    private void BrowseNewPresetDir_Click(object sender, RoutedEventArgs e)
    {
        var path = MainViewModel.BrowseFolder(_vm.NewPresetWorkingDir);
        if (path != null)
            _vm.NewPresetWorkingDir = path;
    }

    // ─── Icon Picker ───────────────────────────────────────────────
    private void IconPickerBtn_Click(object sender, RoutedEventArgs e)
    {
        // Wire up selection handler once
        IconPickerControl.IconSelected -= OnIconSelected;
        IconPickerControl.IconSelected += OnIconSelected;
        IconPickerPopup.IsOpen = true;
    }

    private void OnIconSelected(string icon)
    {
        _vm.NewPresetIcon = icon;
        IconPickerPopup.IsOpen = false;
    }

    // ─── Window chrome ─────────────────────────────────────────────
    private void Minimize_Click(object sender, MouseButtonEventArgs e)
        => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, MouseButtonEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Close_Click(object sender, MouseButtonEventArgs e)
    {
        _vm.CloseAllSessionsCommand.Execute(null);
        Close();
    }

    // Switch maximize icon between □ (restore) and ❐ (maximize)
    private void UpdateMaximizeIcon()
    {
        MaximizeIcon.Text = WindowState == WindowState.Maximized
            ? "\u2752"   // ❒ restore
            : "\u2610";  // ☐ maximize
    }
}