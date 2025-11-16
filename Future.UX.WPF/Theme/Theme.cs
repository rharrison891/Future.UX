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
            { "Border", Color.FromArgb(255,100,100,100) },
            { "Error", Color.FromArgb(255,232,17,35) },
            { "Warning", Color.FromArgb(255,255,185,0) },
            { "Success", Color.FromArgb(255,16,124,16) }
        };

        private static void Test() { 
            
        }
    }
}
