using System.Windows;

namespace Future.UX.Samples.BasicPersonDemo.ViewModels
{
    public partial class WindowViewModel
    {
        private string __title = "Basic Person Demo - Future.UX";
        private void __Minimize(object parameter) => ((Window)parameter).WindowState = WindowState.Minimized;

        private void __ToggleMax(object parameter)
        {
            var window = (Window)parameter;
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        private void __Close(object parameter) => ((Window)parameter).Close();
    }
}
