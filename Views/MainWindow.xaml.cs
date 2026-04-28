using System.Windows;
using System.Windows.Media;
using DevLauncher.Models;
using DevLauncher.ViewModels;

// Explicit aliases to resolve WinForms vs WPF ambiguity
using WinForms      = System.Windows.Forms;
using WpfKey        = System.Windows.Input.Key;
using WpfKeyboard   = System.Windows.Input.Keyboard;
using WpfModifiers  = System.Windows.Input.ModifierKeys;
using KeyEventArgs        = System.Windows.Input.KeyEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

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

    // ─── Terminal Search (Ctrl+F, F3, Shift+F3, Esc) ─────────────

    private readonly List<int> _matchIndices = new();
    private int _currentMatch = -1;
    private string _lastQuery = string.Empty;

    // Ctrl+F on the ScrollViewer opens the search bar
    private void TerminalScroll_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == WpfKey.F && WpfKeyboard.Modifiers == WpfModifiers.Control)
        {
            OpenSearch();
            e.Handled = true;
        }
    }

    private void TerminalSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => RunSearch(TerminalSearchBox.Text);

    private void SearchNext_Click(object sender, RoutedEventArgs e)  => NavigateMatch(+1);
    private void SearchPrev_Click(object sender, RoutedEventArgs e)  => NavigateMatch(-1);
    private void SearchClose_Click(object sender, RoutedEventArgs e) => CloseSearch();

    private void OpenSearch()
    {
        TerminalSearchBar.Visibility = Visibility.Visible;
        TerminalSearchBox.Focus();
        TerminalSearchBox.SelectAll();
        if (!string.IsNullOrEmpty(TerminalSearchBox.Text))
            RunSearch(TerminalSearchBox.Text);
    }

    private void CloseSearch()
    {
        TerminalSearchBar.Visibility = Visibility.Collapsed;
        ClearHighlights();
        _matchIndices.Clear();
        _currentMatch = -1;
        MatchCountLabel.Text = string.Empty;
        TerminalScroll.Focus();
    }

    private void RunSearch(string query)
    {
        ClearHighlights();
        _matchIndices.Clear();
        _currentMatch = -1;

        if (string.IsNullOrWhiteSpace(query))
        {
            MatchCountLabel.Text = string.Empty;
            _vm.SearchMatchCount = 0;
            return;
        }

        _lastQuery = query;
        var items  = GetTerminalTextBlocks();

        for (int i = 0; i < items.Count; i++)
        {
            var tb = items[i];
            if (tb.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                _matchIndices.Add(i);
                HighlightLine(tb, query, isCurrentMatch: false);
            }
        }

        _vm.SearchMatchCount = _matchIndices.Count;

        if (_matchIndices.Count > 0)
        {
            _currentMatch = 0;
            UpdateMatchLabel();
            ScrollToMatch(_matchIndices[0]);
            HighlightCurrentMatch();
        }
        else
        {
            MatchCountLabel.Text = "no matches";
            MatchCountLabel.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
        }
    }

    private void NavigateMatch(int direction)
    {
        if (_matchIndices.Count == 0) return;
        _currentMatch = (_currentMatch + direction + _matchIndices.Count) % _matchIndices.Count;
        UpdateMatchLabel();
        ScrollToMatch(_matchIndices[_currentMatch]);
        HighlightCurrentMatch();
    }

    private void UpdateMatchLabel()
    {
        MatchCountLabel.Text       = $"{_currentMatch + 1} / {_matchIndices.Count} matches";
        MatchCountLabel.Foreground = (System.Windows.Media.Brush)FindResource("Accent3Brush");
    }

    private void ScrollToMatch(int lineIndex)
    {
        var items = GetTerminalTextBlocks();
        if (lineIndex >= 0 && lineIndex < items.Count)
        {
            items[lineIndex].BringIntoView();
        }
    }

    private void HighlightCurrentMatch()
    {
        // Re-highlight all: amber for all matches, bright amber for current
        var items = GetTerminalTextBlocks();
        for (int i = 0; i < _matchIndices.Count; i++)
        {
            int idx = _matchIndices[i];
            if (idx < items.Count)
            {
                bool isCurrent = i == _currentMatch;
                HighlightLine(items[idx], _lastQuery, isCurrentMatch: isCurrent);
            }
        }
    }

    private void ClearHighlights()
    {
        foreach (var tb in GetTerminalTextBlocks())
        {
            // Reset to plain text (remove inline highlight spans)
            string text = tb.Text;
            var type    = tb.Tag is DevLauncher.Models.TerminalLine tl ? tl.Type : DevLauncher.Models.TerminalLineType.Output;
            tb.Background = System.Windows.Media.Brushes.Transparent;
            tb.Inlines.Clear();
            tb.Inlines.Add(new System.Windows.Documents.Run(text)
            {
                Foreground = (System.Windows.Media.Brush)new DevLauncher.Converters.TerminalTypeToColorConverter()
                    .Convert(type, typeof(System.Windows.Media.Brush), string.Empty, System.Globalization.CultureInfo.CurrentCulture)
            });
        }
    }

    private void HighlightLine(System.Windows.Controls.TextBlock tb, string query, bool isCurrentMatch)
    {
        string text = tb.Text;
        var type    = tb.Tag is DevLauncher.Models.TerminalLine tl ? tl.Type : DevLauncher.Models.TerminalLineType.Output;
        var normalFg = (System.Windows.Media.Brush)new DevLauncher.Converters.TerminalTypeToColorConverter()
            .Convert(type, typeof(System.Windows.Media.Brush), string.Empty, System.Globalization.CultureInfo.CurrentCulture);

        var highlightBg = isCurrentMatch
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11))   // bright amber
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 245, 158, 11)); // translucent amber

        var highlightFg = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 12, 16)); // dark text on amber

        tb.Inlines.Clear();
        tb.Background = System.Windows.Media.Brushes.Transparent;

        int pos = 0;
        while (pos < text.Length)
        {
            int idx = text.IndexOf(query, pos, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                // Rest of line — normal color
                tb.Inlines.Add(new System.Windows.Documents.Run(text[pos..]) { Foreground = normalFg });
                break;
            }

            // Text before match
            if (idx > pos)
                tb.Inlines.Add(new System.Windows.Documents.Run(text[pos..idx]) { Foreground = normalFg });

            // Matched text — highlight
            var matchRun = new System.Windows.Documents.Run(text.Substring(idx, query.Length))
            {
                Foreground = highlightFg,
                Background = highlightBg,
            };
            tb.Inlines.Add(matchRun);
            pos = idx + query.Length;
        }
    }

    // Walk the visual tree to get all rendered TextBlocks in the terminal ItemsControl
    private List<System.Windows.Controls.TextBlock> GetTerminalTextBlocks()
    {
        var result = new List<System.Windows.Controls.TextBlock>();
        if (TerminalOutput == null) return result;
        CollectTextBlocks(TerminalOutput, result);
        return result;
    }

    private static void CollectTextBlocks(System.Windows.DependencyObject parent,
        List<System.Windows.Controls.TextBlock> result)
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is System.Windows.Controls.TextBlock tb && tb.Name == "TermLine")
                result.Add(tb);
            else
                CollectTextBlocks(child, result);
        }
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