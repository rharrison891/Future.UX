
using Future.UX.Fonts;
using System.Diagnostics;

namespace Future.UX.SourceGenerators.TEST
{
    public partial class Person
    {
        private string? __title;
        private string? __firstName;
        private string? __lastName;
        private string? __email;
        private string? __phone;
        private string? __address;


        // Command method
        // The generated code will create a public RelayCommand property named PlayCommand
        private void __Play()
        {
            Console.WriteLine($"Running execution code with parameter [NONE]");
        }
        // CanExecute method for the command
        // The generated code will call this method to determine if the command can execute
        partial void CanPlayExecute(ref bool canExecute)
        {
            canExecute = true;
        }
        // Hooks for command execution
        // The generated code will call these methods before and after executing the command
        // If OnPlayExecuting sets cancel to true, the command execution will be aborted
        partial void OnPlayExecuting(ref bool cancel)
        {
            cancel = false;
        }
        partial void OnPlayExecuted()
        {
            Debug.WriteLine($"Executed Play command");
        }


        // Called BEFORE Title property changes
        // Cancelling aborts the property change
        partial void OnFirstNameChanging(string? oldValue, string? newValue, ref bool cancel)
        {
            cancel = false;
        }
        // Called AFTER Title property has changed
        partial void OnFirstNameChanged(string? oldValue, string? newValue)
        {
            Console.WriteLine($"FirstName changed from '{oldValue}' to '{newValue}'");
        }

        // Computed property (depends on field-backed properties)
        public string Fullname => $"{Title ?? ""} {FirstName ?? ""} {LastName ?? ""} [{Email ?? ""}] [{Phone ?? ""}] {Environment.NewLine}{Address ?? ""}";

        // Computed property that depends on another computed property
        public string Greeting => $"Hello, {Fullname ?? ""}";
    }
}