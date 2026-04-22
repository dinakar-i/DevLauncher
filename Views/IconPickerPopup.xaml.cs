// Explicit WPF aliases — required because UseWindowsForms=true causes
// System.Drawing and System.Windows.Forms to conflict with WPF types.
using WpfApp        = System.Windows.Application;
using WpfBrush      = System.Windows.Media.Brush;
using WpfBrushes    = System.Windows.Media.Brushes;
using WpfColor      = System.Windows.Media.Color;
using WpfSolidBrush = System.Windows.Media.SolidColorBrush;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfCursors    = System.Windows.Input.Cursors;
using WpfFontWeight = System.Windows.FontWeights;
using WpfUserControl = System.Windows.Controls.UserControl;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace DevLauncher.Views;

public partial class IconPickerPopup : WpfUserControl
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

    private readonly WpfSolidBrush _normalBg = new(WpfColor.FromRgb(24, 28, 36));
    private readonly WpfSolidBrush _hoverBg  = new(WpfColor.FromRgb(30, 36, 50));

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
                Cursor          = WpfCursors.Hand,
                Background      = active
                    ? (WpfBrush)WpfApp.Current.FindResource("AccentGradient")
                    : new WpfSolidBrush(WpfColor.FromRgb(24, 28, 36)),
                BorderBrush     = active
                    ? WpfBrushes.Transparent
                    : (WpfBrush)WpfApp.Current.FindResource("BorderBrush"),
                BorderThickness = new Thickness(1),
                Tag             = cat,
            };

            border.Child = new TextBlock
            {
                Text             = cat,
                FontFamily       = new WpfFontFamily("Consolas"),
                FontSize         = 10,
                FontWeight       = FontWeights.SemiBold,
                Foreground       = active
                    ? WpfBrushes.White
                    : (WpfBrush)WpfApp.Current.FindResource("Muted2Brush"),
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

        var seen = new HashSet<string>();
        foreach (var icons in Icons.Values)
            foreach (var icon in icons)
                if (seen.Add(icon))
                    IconGrid.Children.Add(CreateIconCell(icon));
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
            BorderBrush     = (WpfBrush)WpfApp.Current.FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
            Cursor          = WpfCursors.Hand,
            ToolTip         = icon,
            Child = new TextBlock
            {
                Text                = icon,
                FontSize            = 20,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment   = System.Windows.VerticalAlignment.Center,
                IsHitTestVisible    = false,
            }
        };

        border.MouseEnter += (_, _) =>
        {
            border.Background  = _hoverBg;
            border.BorderBrush = (WpfBrush)WpfApp.Current.FindResource("AccentBrush");
        };
        border.MouseLeave += (_, _) =>
        {
            border.Background  = _normalBg;
            border.BorderBrush = (WpfBrush)WpfApp.Current.FindResource("BorderBrush");
        };
        border.MouseLeftButtonDown += (_, _) =>
        {
            IconSelected?.Invoke(icon);
            if (Parent is Popup p)
                p.IsOpen = false;
        };

        return border;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (Parent is Popup popup)
            popup.IsOpen = false;
    }
}
