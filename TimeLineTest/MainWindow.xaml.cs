using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TimeLineTest
{

    public partial class MainViewModel
    {
        public DateTime MyEndTime { get; set; } = DateTime.Now.AddDays(1);
        public DateTime MyStartTime { get; set; } = DateTime.Now.AddDays(-1);
        public DateTime MySelectedTime { get; set; } = DateTime.Now;


        public ObservableCollection<(DateTime Start, DateTime End)> MyRecordings { get; set; }  = new ObservableCollection<(DateTime Start, DateTime End)>
        {
            (DateTime.Now.AddDays(-1), DateTime.Now.AddDays(-0.5)),
            (DateTime.Now.AddDays(-0.5), DateTime.Now),
            (DateTime.Now, DateTime.Now.AddHours(1)),
            (DateTime.Now.AddHours(1), DateTime.Now.AddHours(2))
        };

        public MainViewModel()
        {
            // Initialize any properties or commands here if needed
        }
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();

        }

    }
}