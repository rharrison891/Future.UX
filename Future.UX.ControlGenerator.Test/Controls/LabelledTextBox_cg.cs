using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Future.UX.ControlGenerator.Test.Controls
{
    public partial class LabelledTextBox : Control
    {
        partial void TextPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
        partial void OrientationChanged(DependencyPropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
        partial void LabelChanged(DependencyPropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
        partial void OnTemplateApplied()
        {
            PART_Text.TextChanged+=(s, e) =>
            {
                throw new NotImplementedException();
            };
        }
    }
}