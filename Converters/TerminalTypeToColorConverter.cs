using System.Globalization;
using System.Windows.Data;

// Explicit aliases to avoid System.Drawing ambiguity (UseWindowsForms=true)
using WpfColor   = System.Windows.Media.Color;
using WpfBrush   = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfSolidBrush = System.Windows.Media.SolidColorBrush;
using DevLauncher.Models;

namespace DevLauncher.Converters;

public class TerminalTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is TerminalLineType t ? t switch
        {
            TerminalLineType.Command   => new WpfSolidBrush(WpfColor.FromRgb(0,   229, 255)),
            TerminalLineType.Success   => new WpfSolidBrush(WpfColor.FromRgb(16,  185, 129)),
            TerminalLineType.Error     => new WpfSolidBrush(WpfColor.FromRgb(239, 68,  68)),
            TerminalLineType.Warning   => new WpfSolidBrush(WpfColor.FromRgb(245, 158, 11)),
            TerminalLineType.Info      => new WpfSolidBrush(WpfColor.FromRgb(113, 128, 150)),
            TerminalLineType.Separator => new WpfSolidBrush(WpfColor.FromRgb(30,  35,  48)),
            _                          => new WpfSolidBrush(WpfColor.FromRgb(226, 232, 240)),
        } : WpfBrushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
