using System.Windows;

namespace Future.UX.Samples.BasicPersonDemo
{
    public partial class MainWindow : Window
    {
        public Person Person { get; } = new Person() { FirstName = "John", LastName = "Smith" };
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}