using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DevLauncher.Converters;

public class CategoryToBadgeColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string cat = value?.ToString()?.ToLower() ?? "";
        return cat switch
        {
            "devops"  => new SolidColorBrush(Color.FromRgb(124, 58, 237)),
            "data"    => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
            "design"  => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
            _         => new SolidColorBrush(Color.FromRgb(0, 229, 255)),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
