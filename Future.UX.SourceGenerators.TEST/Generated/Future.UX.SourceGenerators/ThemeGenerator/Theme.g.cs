using System;
using System.Collections.Generic;
using System.Windows.Media;
namespace Future.UX.WPF.Theme
{
    public static partial class Theme
    {
        private static readonly Dictionary<string, SolidColorBrush> __modifiedBrushCache = new();
        private static readonly Dictionary<ThemeColor, Color> BaseColors = new()
        {
            [ThemeColor.Background] = __baseColors["Background"],
            [ThemeColor.Foreground] = __baseColors["Foreground"],
            [ThemeColor.Primary] = __baseColors["Primary"],
            [ThemeColor.Secondary] = __baseColors["Secondary"],
            [ThemeColor.Accent] = __baseColors["Accent"],
            [ThemeColor.Border] = __baseColors["Border"],
            [ThemeColor.Error] = __baseColors["Error"],
            [ThemeColor.Warning] = __baseColors["Warning"],
            [ThemeColor.Success] = __baseColors["Success"],
        };
        public static Color GetColor(ThemeColor color) => BaseColors[color];

        public static SolidColorBrush GetBrush(ThemeColor color, double alpha = 1.0, double brightness = 0.0)
        {
            string key = (alpha == 1.0 && brightness == 0.0) ? color.ToString() : $"{color}|{alpha}|{brightness}";

            if (__modifiedBrushCache.TryGetValue(key, out var cached))
                return cached;

            var c = BaseColors[color];
            byte a = (byte)Math.Clamp(c.A * alpha, 0, 255);
            byte r = (byte)Math.Clamp(c.R + brightness, 0, 255);
            byte g = (byte)Math.Clamp(c.G + brightness, 0, 255);
            byte b = (byte)Math.Clamp(c.B + brightness, 0, 255);

            var brush = new SolidColorBrush(Color.FromArgb(a,r,g,b));
            brush.Freeze();
            __modifiedBrushCache[key] = brush;
            return brush;
        }
        public static SolidColorBrush BackgroundBrush => GetBrush(ThemeColor.Background);
        public static Color Background => BaseColors[ThemeColor.Background];
        public static SolidColorBrush ForegroundBrush => GetBrush(ThemeColor.Foreground);
        public static Color Foreground => BaseColors[ThemeColor.Foreground];
        public static SolidColorBrush PrimaryBrush => GetBrush(ThemeColor.Primary);
        public static Color Primary => BaseColors[ThemeColor.Primary];
        public static SolidColorBrush SecondaryBrush => GetBrush(ThemeColor.Secondary);
        public static Color Secondary => BaseColors[ThemeColor.Secondary];
        public static SolidColorBrush AccentBrush => GetBrush(ThemeColor.Accent);
        public static Color Accent => BaseColors[ThemeColor.Accent];
        public static SolidColorBrush BorderBrush => GetBrush(ThemeColor.Border);
        public static Color Border => BaseColors[ThemeColor.Border];
        public static SolidColorBrush ErrorBrush => GetBrush(ThemeColor.Error);
        public static Color Error => BaseColors[ThemeColor.Error];
        public static SolidColorBrush WarningBrush => GetBrush(ThemeColor.Warning);
        public static Color Warning => BaseColors[ThemeColor.Warning];
        public static SolidColorBrush SuccessBrush => GetBrush(ThemeColor.Success);
        public static Color Success => BaseColors[ThemeColor.Success];
    }
}
