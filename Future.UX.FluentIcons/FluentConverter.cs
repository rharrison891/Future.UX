using System.Globalization;
using System.Windows.Data;

namespace Future.UX.FluentIcons
{
    public class FluentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FluentIcons.Icons icon && FluentIconMap.IconMap.TryGetValue(icon, out var glyph))
                return glyph;

            // fallback if value isn't in the map
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
