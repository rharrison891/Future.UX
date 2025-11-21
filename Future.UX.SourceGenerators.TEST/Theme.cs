using Future.UX.Fonts;
using System.Windows.Media;

//Any namespace will work here, as the Source Generator will place the generated code in the same namespace
namespace Future.UX.WPF.Theme
{
    //Has to be public static partial class Theme
    //This ensuures the generated code is placed in the same class
    //The entire application now hass acess to the Theme class and its generated members
    public static partial class Theme
    {
        //User defines the colors with Color.FromArgb method
        //Ensure signature is internal static readonly Dictionary<string, [Color/string]> __baseColors>
        //The dictiinary must be constructed with the new() syntax exactly as shown
        internal static readonly Dictionary<string, Color> __baseColors = new()
        {
            {"Background", Color.FromArgb(255, 30, 30, 30)},
            {"Foreground" , Color.FromArgb(255, 220, 220, 220)},
            {"Primary" , Color.FromArgb(255, 0, 120, 215)},
            {"Secondary", Color.FromArgb(255, 32, 32, 32)},
            {"Accent" , Color.FromArgb(255, 0, 153, 204)},
            { "Border" , Color.FromArgb(255, 100, 100, 100) },
            {"Error", Color.FromArgb(255, 232, 17, 35) },
            {"Warning" , Color.FromArgb(255, 255, 185, 0) },
            {"Success" , Color.FromArgb(255, 16, 124, 16) }
        };

        //User could also define the colors as hex strings

        //internal static readonly Dictionary<string, string> __baseColors = new()
        //{
        //    { "Background","#ff202020" },
        //    { "Foreground" , "#ffdcdcdc" },
        //    { "Primary" , "#ff0078d7" },
        //    { "Secondary", "#ff202020" },
        //    { "Accent" , "#ff0099cc" },
        //    { "Border" , "#ff646464" },
        //    { "Error", "#ffe82123" },
        //    { "Warning" , "#ffffb900" },
        //    { "Success" , "#ff107c10" }
        //};

        //Generated members can now be used throughout the application
        private static void Test()
        {
            
            //Get the color or brush for a theme color
            var brush = Theme.GetBrush(ThemeColor.Primary);
            var color = Theme.GetColor(ThemeColor.Accent);

            //Change the color of an existing theme color
            Theme.SetColor(ThemeColor.Background, "#ff123456");
            Theme.SetColor(ThemeColor.Primary, Color.FromArgb(255, 10, 200, 100));

            //Adjust brushes on the fly using BrushBaseExtension
            //Alpha changes opacity (0-1)
            //brightness changes RGB values clamped to 0-255
            var brushExtension = new BrushBaseExtension(baseColor: ThemeColor.Primary, alpha: 0.5, brightness: -30);
            var generatedBrush = (SolidColorBrush)brushExtension.ProvideValue(null);

            //You can also get the Color if needed
            var colorBrushExtension = new BrushBaseExtension(baseColor: ThemeColor.Success, alpha: 0.75, brightness: 20, asColor: true);
            var generatedColor = (Color)brushExtension.ProvideValue(null);
        }
    }
}