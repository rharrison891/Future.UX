using System.Windows;
using System.Windows.Controls;

namespace Future.UX.ControlGenerator.Test.Controls
{
    public partial class IconButton:Control
    {
        private readonly Icons __Icon = Icons.GlobalNavButton;
        partial void IconChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Icons icon)
                Glyph = FluentMap.IconMap[icon];
        }
    }
}
