using Future.UX.Samples.BasicPersonDemo.ViewModels;
using System.Windows;

namespace Future.UX.Samples.BasicPersonDemo
{
    public partial class MainWindow : Window
    {
        public Person Person { get; } = new Person() 
        { 
            Title="Mr",
            FirstName = "John", 
            LastName = "Smith" ,
            Add1 = "123 Main St",
            Add2 = "Springfield",
            Add3 = "IL",
            Add4 = "USA",
            Postcode = "62701"
        };
        public WindowViewModel WindowViewModel { get; } = new WindowViewModel();
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}