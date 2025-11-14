using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Future.UX.MVVM
{
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T>? _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter = null)
        {
            if (_canExecute == null) return true;
            return _canExecute((T)parameter!);
        }

        public void Execute(object? parameter = null)
        {
            _execute?.Invoke((T)parameter!);
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
    public class RelayCommand : ICommand
    {
        private readonly Action<object>? _execute;
        private readonly Func<object?,bool>? _canExecute;

        public RelayCommand(Action<object> execute, Func<object?,bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;
            return _canExecute(parameter ?? string.Empty);
        }

        public void Execute(object? parameter = null)
        {
            _execute?.Invoke(parameter??string.Empty);
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}