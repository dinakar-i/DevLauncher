using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DevLauncher.Views;

public partial class IconPickerPopup : UserControl
{
    public event Action<string>? IconSelected;

    private string _activeCategory = "Dev";

    private static readonly Dictionary<string, string[]> Icons = new()
    {
        ["Dev"] = new[]
        {
            "🚀","⚡","🔥","💻","🖥️","⌨️","🖱️","🛠️","🔧","🔨",
            "⚙️","🔩","🧩","📦","🗂️","📁","📂","🗃️","💾","💿",
            "🖨️","📟","📠","🔌","🔋","💡","🔦","🕹️","🎮","🖲️"
        },
        ["Code"] = new[]
        {
            "📝","✏️","🖊️","🖋️","📋","📄","📃","📑","🗒️","🗓️",
            "📊","📈","📉","🔢","🔣","🔤","🔡","🔠","🧮","🔐",
            "🔑","🗝️","🔓","🔏","🛡️","⚔️","🧲","🔭","🔬","🧪"
        },
        ["Cloud"] = new[]
        {
            "☁️","🌐","🌍","🌎","🌏","📡","🛰️","🔗","🕸️","📶",
            "📲","📱","☎️","📞","🖧","🗄️","💽","🏗️","🏢","🏭",
            "🌩️","⛅","🌤️","🌥️","🌦️","⛈️","🌧️","🌨️","🌬️","🌪️"
        },
        ["Data"] = new[]
        {
            "📊","📈","📉","🗃️","🗄️","💾","💿","📀","🧮","🔢",
            "📐","📏","🧭","🔍","🔎","🧬","🧫","🧪","⚗️","🔬",
            "🔭","🌡️","⏱️","⏰","🕰️","📅","📆","🗓️","📋","🗺️"
        },
        ["DevOps"] = new[]
        {
            "🐳","☸️","🐧","🦊","🐙","🦑","🐬","🐋","🐘","🦏",
            "⚙️","🔁","🔄","♻️","🔃","🚦","🚥","🛤️","🛣️","🗺️",
            "⛽","🔧","🔨","⛏️","🪛","🪚","🔩","🗜️","⚖️","🧰"
        },
        ["UI"] = new[]
        {
            "🎨","🖌️","🖍️","✏️","📐","📏","🎭","🎪","🎠","🎡",
            "🌈","✨","💫","⭐","🌟","💥","🎯","🎱","🎳","🎰",
            "🃏","🎲","🎴","🎸","🎹","🎺","🎻","🥁","🎷","🎵"
        },
        ["Status"] = new[]
        {
            "✅","❌","⚠️","ℹ️","🚫","🔴","🟡","🟢","🔵","🟣",
            "⬆️","⬇️","⬅️","➡️","↗️","↘️","↙️","↖️","🔝","🔛",
            "🔜","🔚","🔙","🆕","🆗","🆙","🆒","🆓","🆖","🆘"
        },
    };

    // Brushes cached for hover
    private readonly SolidColorBrush _normalBg  = new(Color.FromRgb(24, 28, 36));
    private readonly SolidColorBrush _hoverBg   = new(Color.FromRgb(30, 36, 50));

    public IconPickerPopup()
    {
        InitializeComponent();
        BuildCategoryTabs();
        ShowCategory("Dev");
    }

    private void BuildCategoryTabs()
    {
        CategoryTabs.Children.Clear();
        foreach (var cat in Icons.Keys)
        {
            bool active = cat == _activeCategory;

            var border = new Border
            {
                CornerRadius    = new CornerRadius(6),
                Padding         = new Thickness(12, 6, 12, 6),
                Margin          = new Thickness(0, 0, 4, 0),
                Cursor          = Cursors.Hand,
                Background      = active
                    ? (Brush)Application.Current.FindResource("AccentGradient")
                    : new SolidColorBrush(Color.FromRgb(24, 28, 36)),
                BorderBrush     = active
                    ? Brushes.Transparent
                    : (Brush)Application.Current.FindResource("BorderBrush"),
                BorderThickness = new Thickness(1),
                Tag             = cat,
            };

            border.Child = new TextBlock
            {
                Text           = cat,
                FontFamily     = new FontFamily("Consolas"),
                FontSize       = 10,
                FontWeight     = FontWeights.SemiBold,
                Foreground     = active ? Brushes.White
                    : (Brush)Application.Current.FindResource("Muted2Brush"),
                IsHitTestVisible = false,
            };

            border.MouseLeftButtonDown += (s, _) =>
            {
                if (s is Border b && b.Tag is string c)
                {
                    _activeCategory = c;
                    SearchBox.Text  = string.Empty;
                    BuildCategoryTabs();
                    ShowCategory(c);
                }
            };

            CategoryTabs.Children.Add(border);
        }
    }

    private void ShowCategory(string cat, string filter = "")
    {
        IconGrid.Children.Clear();
        if (!Icons.TryGetValue(cat, out var icons)) return;

        foreach (var icon in icons)
        {
            if (!string.IsNullOrWhiteSpace(filter) &&
                !icon.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
            IconGrid.Children.Add(CreateIconCell(icon));
        }
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        IconGrid.Children.Clear();
        string query = SearchBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            ShowCategory(_activeCategory);
            return;
        }

        // Search all categories
        var seen = new HashSet<string>();
        foreach (var icons in Icons.Values)
            foreach (var icon in icons)
                if (!seen.Contains(icon)) { seen.Add(icon); IconGrid.Children.Add(CreateIconCell(icon)); }
    }

    private UIElement CreateIconCell(string icon)
    {
        var border = new Border
        {
            Width           = 46,
            Height          = 46,
            CornerRadius    = new CornerRadius(8),
            Margin          = new Thickness(2),
            Background      = _normalBg,
            BorderBrush     = (Brush)Application.Current.FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            Cursor          = Cursors.Hand,
            ToolTip         = icon,
            Child = new TextBlock
            {
                Text                = icon,
                FontSize            = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                IsHitTestVisible    = false,
            }
        };

        border.MouseEnter += (_, _) =>
        {
            border.Background   = _hoverBg;
            border.BorderBrush  = (Brush)Application.Current.FindResource("AccentBrush");
        };
        border.MouseLeave += (_, _) =>
        {
            border.Background   = _normalBg;
            border.BorderBrush  = (Brush)Application.Current.FindResource("BorderBrush");
        };
        border.MouseLeftButtonDown += (_, _) =>
        {
            IconSelected?.Invoke(icon);
            if (Parent is System.Windows.Controls.Primitives.Popup p)
                p.IsOpen = false;
        };

        return border;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (Parent is System.Windows.Controls.Primitives.Popup popup)
            popup.IsOpen = false;
    }
}