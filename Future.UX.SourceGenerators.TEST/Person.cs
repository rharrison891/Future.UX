
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

        private Task __Play() {
            return null;
        }
        // Computed property (depends on field-backed properties)
        public string Fullname => $"{Title} {FirstName} {LastName} [{Email}] [{Phone}] {Environment.NewLine}{Address}";

        // Computed property that depends on another computed property
        public string Greeting => $"Hello, {Fullname}";
    }
}