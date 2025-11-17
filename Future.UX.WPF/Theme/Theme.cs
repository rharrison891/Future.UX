using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Future.UX.WPF.Theme
{
    public static partial class Theme
    {
        private static readonly Dictionary<string, Color> __baseColors = new() {
            { "Background",Color.FromArgb(255,30,30,30)  },
            { "Foreground", Color.FromArgb(255,220,220,220) },
            { "Primary", Color.FromArgb(255,0,120,215) },
            { "Secondary", Color.FromArgb(255,32,32,32) },
            { "Accent", Color.FromArgb(255,0,153,204) },
            { "Accent2", Color.FromArgb(255,30,153,204) },
            { "Border", Color.FromArgb(255,100,100,100) },
            { "Error", Color.FromArgb(255,232,17,35) },
            { "Warning", Color.FromArgb(255,255,185,0) },
            { "Success", Color.FromArgb(255,16,124,16) }
        };

        private static void Generated() { 
            var brush = Theme.GetBrush(ThemeColor.Primary);
            var color= Theme.GetColor(ThemeColor.Accent);
            var brushExtension=new BrushBaseExtension(baseColor: ThemeColor.Background, alpha: 0.5, brightness:-20);
            var modBrush= brushExtension.ProvideValue(null);
            var colorExtension = new BrushBaseExtension(baseColor: ThemeColor.Error, alpha: 0.8, brightness: 30, asColor: true);
            var modColor = colorExtension.ProvideValue(null);
        }
    }
}
