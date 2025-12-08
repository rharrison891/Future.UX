using System.Globalization;
using System.Windows.Data;

public class FluentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Icons icon && FluentMap.IconMap.TryGetValue(icon, out var glyph))
            return glyph;

        // fallback if value isn't in the map
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
