
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

        private void __Play() {
            Console.WriteLine($"Running execution code with parameter [NONE]");
        }


        partial void OnFirstNameChanging(string oldValue, string newValue, ref bool cancel)
        {
            cancel = false;
        }
        partial void OnFirstNameChanged(string oldValue, string newValue)
        {
            Console.WriteLine($"FirstName changed from '{oldValue}' to '{newValue}'");
        }

        // Computed property (depends on field-backed properties)
        public string Fullname => $"{Title} {FirstName} {LastName} [{Email}] [{Phone}] {Environment.NewLine}{Address}";

        // Computed property that depends on another computed property
        public string Greeting => $"Hello, {Fullname}";
    }
}