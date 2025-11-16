using System;
using System.Windows.Markup;
using System.Windows.Media;
using Future.UX.WPF.Theme;
namespace Future.UX.WPF.Markup
{
    [MarkupExtensionReturnType(typeof(Brush))]
    public partial class BrushBaseExtension : MarkupExtension
    {
        public ThemeColor Base { get; set; }
        public double Alpha { get; set; } = 1.0;
        public double Brightness { get; set; } = 0.0;
        public BrushBaseExtension() { }
        public override object ProvideValue(IServiceProvider serviceProvider) => Theme.Theme.GetBrush(Base, Alpha, Brightness);

        public static BrushBaseExtension Background(double alpha = 1.0, double brightness = 0.0) =>
            new BrushBaseExtension { Base = ThemeColor.Background, Alpha = alpha, Brightness = brightness };

        public static BrushBaseExtension Foreground(double alpha = 1.0, double brightness = 0.0) =>
            new BrushBaseExtension { Base = ThemeColor.Foreground, Alpha = alpha, Brightness = brightness };

        public static BrushBaseExtension Primary(double alpha = 1.0, double brightness = 0.0) =>
            new BrushBaseExtension { Base = ThemeColor.Primary, Alpha = alpha, Brightness = brightness };

        public static BrushBaseExtension Secondary(double alpha = 1.0, double brightness = 0.0) =>
            new BrushBaseExtension { Base = ThemeColor.Secondary, Alpha = alpha, Brightness = brightness };

        public static BrushBaseExtension Accent(double alpha = 1.0, double brightness = 0.0) =>
            new BrushBaseExtension { Base = ThemeColor.Accent, Alpha = alpha, Brightness = brightness };

        public static BrushBaseExtension Border(double alpha = 1.0, double brightness = 0.0) =>
            new BrushBaseExtension { Base = ThemeColor.Border, Alpha = alpha, Brightness = brightness };

        public static BrushBaseExtension Error(double alpha = 1.0, double brightness = 0.0) =>
            new BrushBaseExtension { Base = ThemeColor.Error, Alpha = alpha, Brightness = brightness };

        public static BrushBaseExtension Warning(double alpha = 1.0, double brightness = 0.0) =>
            new BrushBaseExtension { Base = ThemeColor.Warning, Alpha = alpha, Brightness = brightness };

        public static BrushBaseExtension Success(double alpha = 1.0, double brightness = 0.0) =>
            new BrushBaseExtension { Base = ThemeColor.Success, Alpha = alpha, Brightness = brightness };
    }
}
