using System.Globalization;
using System.Windows.Data;

// Explicit aliases to avoid System.Drawing ambiguity (UseWindowsForms=true)
using WpfColor      = System.Windows.Media.Color;
using WpfBrush      = System.Windows.Media.Brush;
using WpfSolidBrush = System.Windows.Media.SolidColorBrush;

namespace DevLauncher.Converters;

public class CategoryToBadgeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string cat = value?.ToString()?.ToLower() ?? "";
        return cat switch
        {
            "devops" => new WpfSolidBrush(WpfColor.FromRgb(124, 58,  237)),
            "data"   => new WpfSolidBrush(WpfColor.FromRgb(16,  185, 129)),
            "design" => new WpfSolidBrush(WpfColor.FromRgb(245, 158, 11)),
            _        => new WpfSolidBrush(WpfColor.FromRgb(0,   229, 255)),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
