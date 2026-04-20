using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DevLauncher.Models;

namespace DevLauncher.Converters;

public class TerminalTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is TerminalLineType t ? t switch
        {
            TerminalLineType.Command   => new SolidColorBrush(Color.FromRgb(0,   229, 255)),  // cyan
            TerminalLineType.Success   => new SolidColorBrush(Color.FromRgb(16,  185, 129)),  // green
            TerminalLineType.Error     => new SolidColorBrush(Color.FromRgb(239, 68,  68)),   // red
            TerminalLineType.Warning   => new SolidColorBrush(Color.FromRgb(245, 158, 11)),   // amber
            TerminalLineType.Info      => new SolidColorBrush(Color.FromRgb(113, 128, 150)),  // muted
            TerminalLineType.Separator => new SolidColorBrush(Color.FromRgb(30,  35,  48)),   // border
            _                          => new SolidColorBrush(Color.FromRgb(226, 232, 240)),  // text
        } : Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
