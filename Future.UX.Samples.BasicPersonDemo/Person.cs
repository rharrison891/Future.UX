namespace Future.UX.Samples.BasicPersonDemo
{
    public partial class Person
    {
        //Simple setup for fields
        private string? __firstName;
        private string? __lastName;
        private bool __buttonEnabled = true;
        private int __clickCount;

        //Commands
        private void __ClickIncrementer(object parameter) => ClickCount += Convert.ToInt32(parameter);
        private bool __ClickIncrementer_Can(object parameter) => ButtonEnabled;

        //Auto notifying computed properties
        public string FullName => $"{FirstName} {LastName}";
        public string ClickCountText => $"Clicked {ClickCount} times";
    }
}
