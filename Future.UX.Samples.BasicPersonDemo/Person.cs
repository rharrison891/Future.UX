using System.Windows;

namespace Future.UX.Samples.BasicPersonDemo
{
    public partial class Person
    {
        //Simple setup for fields
        private string? __title;
        private string? __firstName;
        private string? __lastName;
        private string? __add1;
        private string? __add2;
        private string? __add3;
        private string? __add4;
        private string? __postcode;
        
        //Auto notifying computed properties
        public string FullName => $"{Title} {FirstName} {LastName}";
        public string ShortName => $"{Title} {FirstName.ToArray().First()} {LastName}";
        public string Greeting => $"Dear {Title} {LastName},";
        public string AddressBlock=> $"{Add1}, \n{Add2}, \n{Add3}, \n{Add4}, \n{Postcode.ToUpper()}";
    }
}
