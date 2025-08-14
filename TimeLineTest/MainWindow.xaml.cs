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
using static TimeLineTest.RecordingBar;

namespace TimeLineTest
{

    public partial class MainViewModel
    {
        public DateTime MyEndTime { get; set; } = DateTime.Now.AddDays(1);
        public DateTime MyStartTime { get; set; } = DateTime.Now.AddDays(-1);
        public DateTime MySelectedTime { get; set; } = DateTime.Now;
        public ObservableCollection<HourMask> MyHourMasks { get; set; } = new ObservableCollection<HourMask>();


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
            // 예시: 10분~20분, 30분~35분, 50분~59분 구간에 녹화 마스크 설정
            DateTime now = DateTime.Now;
            HourMask hourMask1 = new HourMask
            {
                dateTime = new DateTime(now.Year, now.Month, now.Day, 14, 10, 0) // 현재 시간의 시각을 기준으로 설정
            };
            HourMask MyHourMask2 = new HourMask
            {
                dateTime = new DateTime(now.Year, now.Month, now.Day, 10, 30, 0) // 현재 시간의 시각을 기준으로 설정
            };
            HourMask MyHourMask3 = new HourMask
            {
                dateTime = new DateTime(now.Year, now.Month, now.Day, 15, 50, 0) // 현재 시간의 시각을 기준으로 설정
            };
            SetRecording(hourMask1, 10, 0, 20, 0);
            SetRecording(MyHourMask2, 30, 0, 35, 0);
            SetRecording(MyHourMask3, 50, 0, 59, 59);
            SetRecording(hourMask1, 50, 0, 59, 59);
            MyHourMasks.Add(hourMask1);
            MyHourMasks.Add(MyHourMask2);
            MyHourMasks.Add(MyHourMask3);

        }

        private void SetRecording(HourMask mask, int startMin, int startSec, int endMin, int endSec)
        {
            int start = startMin * 60 + startSec;
            int end = endMin * 60 + endSec;
            for (int i = start; i <= end; i++)
            {
                int m = i / 60;
                int s = i % 60;
                mask.min[m].sec[s].mask = 1; // 녹화됨 표시
            }

            
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