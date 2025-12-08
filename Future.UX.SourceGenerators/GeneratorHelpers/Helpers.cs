using System.Collections.Generic;

public static class Helpers
{
    public static Dictionary<string, string> UsingsMap = new()
    {
        ["Brush"] = "System.Windows.Media",
        ["Color"] = "System.Windows.Media",
        ["Thickness"] = "System.Windows",
        ["CornerRadius"] = "System.Windows",
        ["ICommand"] = "System.Windows.Input",
        ["ObservableCollection"] = "System.Collections.ObjectModel"
    };
}
